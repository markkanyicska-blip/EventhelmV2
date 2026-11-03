using System.Buffers.Binary;
using System.Text.Json;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace EventHelm.Api.Tests;

public sealed class AccountTests
{
    private const string Password = "synthetic-account-passphrase";
    private readonly MemoryAccountRepository _repository = new();
    private readonly TestClock _clock = new();
    private readonly AccountPasswordHasher _hasher = new();
    private AccountService Service => new(_repository, _hasher, _clock);

    [Fact]
    public async Task Creation_preserves_user_and_creates_distinct_pending_identity()
    {
        var user = _repository.User;
        var account = await Service.CreatePendingAsync(user.Id);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.NotEqual(user.Id, account.Id);
        Assert.Equal(user.Id, account.UserId);
        Assert.Equal(AccountStatus.Pending, account.Status);
        Assert.Null(account.PasswordHash);
        Assert.Equal(_clock.GetUtcNow(), account.CreatedAt);
        Assert.Equal(account.CreatedAt, account.UpdatedAt);
        Assert.Equal(0, account.Version);
        Assert.Same(user, _repository.User);
        Assert.Equal("Operational label only", user.Role);
        Assert.False(await Service.VerifyCredentialsAsync(account.Id, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.CreatePendingAsync(user.Id));
    }

    [Fact]
    public async Task Creation_rejects_missing_or_empty_user()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.CreatePendingAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentException>(() => Service.CreatePendingAsync(Guid.Empty));
        Assert.Empty(_repository.Accounts);
    }

    [Fact]
    public async Task Activation_hashes_password_and_verification_is_read_only_when_current()
    {
        var pending = await Service.CreatePendingAsync(_repository.User.Id);
        _clock.Advance(TimeSpan.FromMinutes(1));
        var active = await Service.ActivateAsync(pending.Id, Password);

        Assert.Equal(AccountStatus.Active, active.Status);
        Assert.Equal(1, active.Version);
        Assert.Equal(pending.CreatedAt, active.CreatedAt);
        Assert.Equal(_clock.GetUtcNow(), active.UpdatedAt);
        Assert.NotNull(active.PasswordHash);
        Assert.DoesNotContain(Password, active.PasswordHash);
        Assert.True(await Service.VerifyCredentialsAsync(active.Id, Password));
        Assert.False(await Service.VerifyCredentialsAsync(active.Id, "wrong-password"));
        Assert.False(await Service.VerifyCredentialsAsync(active.Id, ""));
        Assert.False(await Service.VerifyCredentialsAsync(active.Id, new string('x', 1025)));
        Assert.False(await Service.VerifyCredentialsAsync(Guid.NewGuid(), Password));
        Assert.Equal(active, _repository.Accounts[active.Id]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.ActivateAsync(active.Id, Password));
    }

    [Fact]
    public void Identity_hashes_are_salted_versioned_and_use_configured_work_factor()
    {
        var account = NewAccount();
        var first = _hasher.HashPassword(account, Password);
        var second = _hasher.HashPassword(account, Password);
        var bytes = Convert.FromBase64String(first);

        Assert.NotEqual(first, second);
        Assert.Equal(1, bytes[0]);
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4)));
        Assert.Equal((uint)AccountPasswordHasher.IterationCount,
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4)));
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(account, first, Password));
        Assert.Equal(PasswordVerificationResult.Failed,
            _hasher.VerifyHashedPassword(account, first, "incorrect"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("               ")]
    public async Task Invalid_new_password_does_not_activate(string password)
    {
        var pending = await Service.CreatePendingAsync(_repository.User.Id);
        await Assert.ThrowsAsync<ArgumentException>(() => Service.ActivateAsync(pending.Id, password));
        Assert.Equal(pending, _repository.Accounts[pending.Id]);
    }

    [Fact]
    public async Task Password_bounds_and_spaces_are_preserved()
    {
        var pending = await Service.CreatePendingAsync(_repository.User.Id);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Service.ActivateAsync(pending.Id, new string('x', 1025)));
        var password = " " + new string('x', 1022) + " ";
        var active = await Service.ActivateAsync(pending.Id, password);
        Assert.True(await Service.VerifyCredentialsAsync(active.Id, password));
        Assert.False(await Service.VerifyCredentialsAsync(active.Id, password.Trim()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Suspension_blocks_pending_or_active_accounts(bool activate)
    {
        var account = await Service.CreatePendingAsync(_repository.User.Id);
        if (activate)
            account = await Service.ActivateAsync(account.Id, Password);

        var suspended = await Service.SuspendAsync(account.Id);
        Assert.Equal(AccountStatus.Suspended, suspended.Status);
        Assert.Equal(account.Version + 1, suspended.Version);
        Assert.False(await Service.VerifyCredentialsAsync(account.Id, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.ActivateAsync(account.Id, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.SuspendAsync(account.Id));
    }

    [Fact]
    public async Task Missing_or_inactive_linked_user_blocks_activation_and_verification()
    {
        var pending = await Service.CreatePendingAsync(_repository.User.Id);
        _repository.User.IsActive = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.ActivateAsync(pending.Id, Password));
        _repository.User.IsActive = true;
        var active = await Service.ActivateAsync(pending.Id, Password);
        _repository.User.IsActive = false;
        Assert.False(await Service.VerifyCredentialsAsync(active.Id, Password));
        _repository.HideUser = true;
        Assert.False(await Service.VerifyCredentialsAsync(active.Id, Password));
    }

    [Fact]
    public async Task Missing_accounts_are_explicit_lifecycle_errors()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.ActivateAsync(Guid.NewGuid(), Password));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.SuspendAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Successful_verification_upgrades_older_identity_hash()
    {
        var account = await Service.CreatePendingAsync(_repository.User.Id);
        var olderHasher = new PasswordHasher<Account>(Options.Create(new PasswordHasherOptions
        {
            IterationCount = 10_000
        }));
        account = account with
        {
            Status = AccountStatus.Active,
            PasswordHash = olderHasher.HashPassword(account, Password)
        };
        _repository.Accounts[account.Id] = account;

        Assert.False(await Service.VerifyCredentialsAsync(account.Id, "incorrect"));
        Assert.Equal(account, _repository.Accounts[account.Id]);
        Assert.True(await Service.VerifyCredentialsAsync(account.Id, Password));
        var updated = _repository.Accounts[account.Id];
        Assert.NotEqual(account.PasswordHash, updated.PasswordHash);
        Assert.Equal(account.Version + 1, updated.Version);
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(updated, updated.PasswordHash!, Password));
    }

    [Fact]
    public async Task Concurrent_update_does_not_overwrite_state()
    {
        var account = await Service.CreatePendingAsync(_repository.User.Id);
        _repository.RejectUpdates = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.ActivateAsync(account.Id, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.SuspendAsync(account.Id));
        Assert.Equal(account, _repository.Accounts[account.Id]);
    }

    [Fact]
    public async Task Rehash_conflict_is_not_reported_as_success()
    {
        var account = await Service.CreatePendingAsync(_repository.User.Id);
        var olderHasher = new PasswordHasher<Account>(Options.Create(new PasswordHasherOptions
        {
            IterationCount = 10_000
        }));
        account = account with
        {
            Status = AccountStatus.Active,
            PasswordHash = olderHasher.HashPassword(account, Password)
        };
        _repository.Accounts[account.Id] = account;
        _repository.RejectUpdates = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Service.VerifyCredentialsAsync(account.Id, Password));
        Assert.Equal(account, _repository.Accounts[account.Id]);
    }

    [Fact]
    public async Task Repository_failures_and_cancellation_propagate()
    {
        _repository.FailReads = true;
        await Assert.ThrowsAsync<IOException>(() => Service.VerifyCredentialsAsync(Guid.NewGuid(), Password));
        _repository.FailReads = false;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Service.CreatePendingAsync(_repository.User.Id, cancellation.Token));
    }

    [Fact]
    public void Hash_is_omitted_from_json_and_record_string()
    {
        var account = NewAccount() with { PasswordHash = "synthetic-hash-not-for-output" };
        Assert.DoesNotContain(account.PasswordHash, JsonSerializer.Serialize(account));
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(account));
        Assert.DoesNotContain(account.PasswordHash, account.ToString());
    }

    private static Account NewAccount() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private sealed class MemoryAccountRepository : IAccountRepository
    {
        public User User { get; } = new()
        {
            Id = Guid.NewGuid(),
            Name = "Synthetic Person",
            Email = "synthetic@example.invalid",
            Role = "Operational label only"
        };
        public Dictionary<Guid, Account> Accounts { get; } = [];
        public bool HideUser { get; set; }
        public bool RejectUpdates { get; set; }
        public bool FailReads { get; set; }

        public Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<User?>(!HideUser && User.Id == userId ? User : null);
        }

        public Task<Account?> GetAsync(Guid accountId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailReads)
                throw new IOException("Synthetic store failure.");
            return Task.FromResult(Accounts.GetValueOrDefault(accountId));
        }

        public Task<bool> TryCreateAsync(Account account, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Accounts.Values.Any(existing => existing.UserId == account.UserId))
                return Task.FromResult(false);
            return Task.FromResult(Accounts.TryAdd(account.Id, account));
        }

        public Task<bool> TryUpdateAsync(
            Account account, long expectedVersion, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (RejectUpdates || !Accounts.TryGetValue(account.Id, out var existing) ||
                existing.Version != expectedVersion)
                return Task.FromResult(false);
            Accounts[account.Id] = account;
            return Task.FromResult(true);
        }
    }
}
