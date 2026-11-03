using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

// Mutations and their audit records must commit together. Redemption also activates
// the account atomically, after rechecking account/user state and token validity.
public interface IAccountInvitationRepository
{
    Task<AccountInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<bool> TryIssueAsync(AccountInvitation invitation, CancellationToken cancellationToken);
    Task<bool> TryRevokeAsync(Guid invitationId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> TryRedeemAsync(
        AccountInvitation invitation, Account activatedAccount, long expectedVersion,
        DateTimeOffset now, CancellationToken cancellationToken);
}
