using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using Microsoft.AspNetCore.Identity;

namespace EventHelm.Api.Services;

// Internal foundations only: no registration, delivery transport, endpoints or sessions.
public sealed class AccountInvitationService(
    IAccountRepository accounts,
    IAccountInvitationRepository invitations,
    IPasswordHasher<Account> passwordHasher,
    TimeProvider clock)
{
    public async Task<IssuedAccountInvitation> IssueAsync(
        Guid accountId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("An account UUID is required.", nameof(accountId));
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromDays(7))
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Lifetime must be positive and at most seven days.");

        var token = AccountInvitationToken.Generate();
        var now = clock.GetUtcNow();
        var invitation = new AccountInvitation
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            TokenHash = AccountInvitationToken.Hash(token),
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime)
        };
        if (!await invitations.TryIssueAsync(invitation, cancellationToken))
            throw new InvalidOperationException("Invitation issuance requires a pending account and active linked user.");

        return new(invitation.Id, accountId, invitation.ExpiresAt, token);
    }

    public async Task RevokeAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        if (invitationId == Guid.Empty)
            throw new ArgumentException("An invitation UUID is required.", nameof(invitationId));
        if (!await invitations.TryRevokeAsync(invitationId, clock.GetUtcNow(), cancellationToken))
            throw new InvalidOperationException("The invitation does not exist or is already terminal.");
    }

    public async Task<Account> RedeemAsync(
        string token, string password, CancellationToken cancellationToken = default)
    {
        AccountService.ValidateNewPassword(password);
        var invitation = await invitations.GetByTokenHashAsync(
            AccountInvitationToken.Hash(token), cancellationToken);
        var now = clock.GetUtcNow();
        if (invitation is null || invitation.RedeemedAt is not null ||
            invitation.RevokedAt is not null || invitation.ExpiresAt <= now)
            throw new InvalidOperationException("The invitation is invalid or unavailable.");

        var account = await accounts.GetAsync(invitation.AccountId, cancellationToken);
        if (account?.Status != AccountStatus.Pending)
            throw new InvalidOperationException("The invitation is invalid or unavailable.");

        var hash = passwordHasher.HashPassword(account, password);
        now = clock.GetUtcNow();
        var activated = account with
        {
            Status = AccountStatus.Active,
            PasswordHash = hash,
            UpdatedAt = now,
            Version = checked(account.Version + 1)
        };
        if (!await invitations.TryRedeemAsync(invitation, activated, account.Version, now, cancellationToken))
            throw new InvalidOperationException("The invitation is invalid or unavailable.");

        return activated;
    }
}
