using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

// Implementations must enforce unique UserId and atomic compare-and-swap updates.
public interface IAccountRepository
{
    Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Account?> GetAsync(Guid accountId, CancellationToken cancellationToken);
    Task<bool> TryCreateAsync(Account account, CancellationToken cancellationToken);
    Task<bool> TryUpdateAsync(
        Account account, long expectedVersion, CancellationToken cancellationToken);
}
