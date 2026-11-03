using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

// Durable, secret-free events are appended inside repository transactions.
// This read abstraction permits a future authorized audit consumer without exposing SQL.
public interface IAccountAuditLog
{
    Task<IReadOnlyList<AccountAuditEvent>> GetForAccountAsync(
        Guid accountId, CancellationToken cancellationToken);
}
