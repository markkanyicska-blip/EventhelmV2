using System.Security.Cryptography;
using System.Text;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using Microsoft.AspNetCore.Identity;

namespace EventHelm.Api.Services;

// Dormant: does not issue cookies, claims, roles or shared-password tickets.
public sealed class IndividualAuthenticationService
{
    private readonly IAccountRepository _accounts;
    private readonly IAccountSessionRepository _sessions;
    private readonly IPasswordHasher<Account> _hasher;
    private readonly TimeProvider _clock;
    private readonly Account _dummyAccount;
    private readonly string _dummyHash;

    public IndividualAuthenticationService(
        IAccountRepository accounts, IAccountSessionRepository sessions,
        IPasswordHasher<Account> hasher, TimeProvider clock)
    {
        _accounts = accounts;
        _sessions = sessions;
        _hasher = hasher;
        _clock = clock;
        _dummyAccount = new Account
        {
            Id = Guid.Empty,
            UserId = Guid.Empty,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };
        _dummyHash = hasher.HashPassword(_dummyAccount, "synthetic-dummy-verification-only");
    }

    // Null is the sole credential/eligibility/lockout rejection result.
    // Storage failures propagate; an integration must never accept a cached identity on error.
    public async Task<IssuedAccountSession?> AuthenticateAsync(
        string? email, string? password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = AccountLoginEmail.Normalize(email);
        if (normalized is null || string.IsNullOrEmpty(password) || password.Length > 1024)
            return null;
        var identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        var now = _clock.GetUtcNow();
        if (!await _sessions.TryReserveAttemptAsync(identifierHash, now, cancellationToken))
            return null;

        var login = await _sessions.FindLoginAsync(normalized, cancellationToken);
        var account = login?.Account;
        var user = account is null ? null : await _accounts.GetUserAsync(account.UserId, cancellationToken);
        var eligible = account?.Status == AccountStatus.Active &&
            !string.IsNullOrEmpty(account.PasswordHash) && user?.IsActive == true;
        var verification = _hasher.VerifyHashedPassword(
            eligible ? account! : _dummyAccount, eligible ? account!.PasswordHash! : _dummyHash, password);
        cancellationToken.ThrowIfCancellationRequested();
        now = _clock.GetUtcNow();
        if (!eligible || verification == PasswordVerificationResult.Failed)
        {
            await _sessions.RecordFailureAsync(identifierHash, now, cancellationToken);
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var updated = account! with
            {
                PasswordHash = _hasher.HashPassword(account, password),
                Version = checked(account.Version + 1),
                UpdatedAt = now
            };
            if (!await _accounts.TryUpdateAsync(updated, account.Version, cancellationToken))
                return null;
            login = login! with { Account = updated };
        }

        // Separate random namespace from invitations; only the hash enters persistence.
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
        now = _clock.GetUtcNow();
        var expiresAt = now.AddHours(8);
        if (!await _sessions.TryCreateSessionAsync(login!, identifierHash, tokenHash, now, expiresAt, cancellationToken))
            return null;
        return new(login!.Account.Id, expiresAt, token);
    }
}
