using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

public interface IAccountSessionRepository
{
    // Trusted provisioning only; never inferred from editable Users.Email.
    Task BindLoginEmailAsync(Guid accountId, string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> TryReserveAttemptAsync(string identifierHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordFailureAsync(string identifierHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task<AccountLogin?> FindLoginAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> TryCreateSessionAsync(
        AccountLogin login, string identifierHash, string tokenHash, DateTimeOffset now,
        DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<ValidAccountSession?> ValidateAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task RevokeAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task RevokeAllAsync(Guid accountId, DateTimeOffset now, CancellationToken cancellationToken);
}
