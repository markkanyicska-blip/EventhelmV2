using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace EventHelm.Api.Services;

// Deliberately not registered in production: these operations do not authenticate HTTP requests.
public sealed class AccountService(
    IAccountRepository repository,
    IPasswordHasher<Account> passwordHasher,
    TimeProvider clock)
{
    public async Task<Account> CreatePendingAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A user UUID is required.", nameof(userId));

        if (await repository.GetUserAsync(userId, cancellationToken) is null)
            throw new KeyNotFoundException("The linked user does not exist.");

        var now = clock.GetUtcNow();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (!await repository.TryCreateAsync(account, cancellationToken))
            throw new InvalidOperationException("The user already has an account.");

        return account;
    }

    public async Task<Account> ActivateAsync(
        Guid accountId, string password, CancellationToken cancellationToken = default)
    {
        ValidateNewPassword(password);
        var account = await GetRequiredAsync(accountId, cancellationToken);
        if (account.Status != AccountStatus.Pending)
            throw new InvalidOperationException("Only a pending account can be activated.");

        var user = await repository.GetUserAsync(account.UserId, cancellationToken);
        if (user?.IsActive != true)
            throw new InvalidOperationException("Activation requires an active linked user.");

        return await SaveAsync(account, account with
        {
            Status = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword(account, password)
        }, cancellationToken);
    }

    public async Task<Account> SuspendAsync(
        Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAsync(accountId, cancellationToken);
        if (account.Status is not (AccountStatus.Pending or AccountStatus.Active))
            throw new InvalidOperationException("Only a pending or active account can be suspended.");

        return await SaveAsync(account, account with
        {
            Status = AccountStatus.Suspended
        }, cancellationToken);
    }

    public async Task<bool> VerifyCredentialsAsync(
        Guid accountId, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password) || password.Length > 1024)
            return false;

        var account = await repository.GetAsync(accountId, cancellationToken);
        if (account?.Status != AccountStatus.Active ||
            string.IsNullOrEmpty(account.PasswordHash))
            return false;

        var user = await repository.GetUserAsync(account.UserId, cancellationToken);
        if (user?.IsActive != true)
            return false;

        var result = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return false;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await SaveAsync(account, account with
            {
                PasswordHash = passwordHasher.HashPassword(account, password)
            }, cancellationToken);
        }

        return true;
    }

    private async Task<Account> GetRequiredAsync(
        Guid accountId, CancellationToken cancellationToken) =>
        await repository.GetAsync(accountId, cancellationToken)
        ?? throw new KeyNotFoundException("The account does not exist.");

    private async Task<Account> SaveAsync(
        Account original, Account updated, CancellationToken cancellationToken)
    {
        updated = updated with
        {
            UpdatedAt = clock.GetUtcNow(),
            Version = checked(original.Version + 1)
        };
        if (!await repository.TryUpdateAsync(updated, original.Version, cancellationToken))
            throw new InvalidOperationException("The account changed concurrently; reload before retrying.");

        return updated;
    }

    private static void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 15 or > 1024)
            throw new ArgumentException("Passwords must contain 15 to 1024 characters.", nameof(password));
    }
}
