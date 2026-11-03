using System.Security.Cryptography;
using System.Text;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public sealed class AccountSessionService(IAccountSessionRepository repository, TimeProvider clock)
{
    public Task<ValidAccountSession?> ValidateAsync(string? token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Hash(token) is { } hash
            ? repository.ValidateAsync(hash, clock.GetUtcNow(), cancellationToken)
            : Task.FromResult<ValidAccountSession?>(null);
    }

    public Task RevokeAsync(string token, CancellationToken cancellationToken = default) =>
        repository.RevokeAsync(Hash(token) ?? throw new ArgumentException("Invalid session token format.", nameof(token)),
            clock.GetUtcNow(), cancellationToken);

    // Trusted internal operation: a future endpoint must authorize the account ID.
    public Task RevokeAllAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("An account UUID is required.", nameof(accountId));
        return repository.RevokeAllAsync(accountId, clock.GetUtcNow(), cancellationToken);
    }

    private static string? Hash(string? token) =>
        token is { Length: 64 } && token.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F')
            ? Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token))) : null;
}
