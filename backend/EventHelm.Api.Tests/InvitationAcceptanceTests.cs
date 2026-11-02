using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using Xunit;

namespace EventHelm.Api.Tests;

public sealed class InvitationAcceptanceTests(AccountPostgresFixture database) : IClassFixture<AccountPostgresFixture>
{
    private const string Password = "synthetic-initial-passphrase";
    private readonly TestClock _clock = new();
    private readonly AccountPasswordHasher _hasher = new();
    private AccountRepository Repository => new(database.DataSource);
    private AccountInvitationService Service => new(Repository, Repository, _hasher, _clock);

    [Theory]
    [InlineData(15)]
    [InlineData(1024)]
    public async Task Acceptance_persists_identity_hash_and_preserves_password_and_user(int length)
    {
        var (pending, invitation) = await CreateInvitationAsync();
        var originalUser = await Repository.GetUserAsync(pending.UserId, default);
        var password = " " + new string('\u00e9', length - 2) + " ";
        _clock.Advance(TimeSpan.FromMinutes(1));

        var accepted = await Service.RedeemAsync(invitation.Token, password);

        Assert.Equal(pending.Id, accepted.Id);
        Assert.Equal(pending.UserId, accepted.UserId);
        Assert.Equal(pending.CreatedAt, accepted.CreatedAt);
        Assert.Equal(_clock.GetUtcNow(), accepted.UpdatedAt);
        Assert.Equal(AccountStatus.Active, accepted.Status);
        Assert.Equal(1, accepted.Version);
        Assert.Equal(accepted, await Repository.GetAsync(pending.Id, default));
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(accepted, accepted.PasswordHash!, password));
        Assert.Equal(PasswordVerificationResult.Failed,
            _hasher.VerifyHashedPassword(accepted, accepted.PasswordHash!, password.Trim()));
        var storedToken = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(invitation.Token), default);
        Assert.Equal(accepted.UpdatedAt, storedToken!.RedeemedAt);
        var audit = await Repository.GetForAccountAsync(pending.Id, default);
        Assert.Equal(3, audit.Count);
        var redemption = Assert.Single(audit, entry => entry.EventType == "InvitationRedeemed");
        Assert.Equal(invitation.Id, redemption.InvitationId);
        Assert.Equal(accepted.UpdatedAt, redemption.OccurredAt);
        var user = await Repository.GetUserAsync(pending.UserId, default);
        Assert.Equal(originalUser!.Name, user!.Name);
        Assert.Equal(originalUser.Email, user.Email);
        Assert.Equal(originalUser.Role, user.Role);
        Assert.Equal(originalUser.IsActive, user.IsActive);
        Assert.Equal(originalUser.CreatedAt, user.CreatedAt);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RedeemAsync(invitation.Token, Password));
        Assert.Equal(accepted, await Repository.GetAsync(pending.Id, default));
        Assert.Equal(audit, await Repository.GetForAccountAsync(pending.Id, default));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("               ")]
    [InlineData("too-long")]
    public async Task Invalid_password_never_hashes_or_consumes_invitation(string? input)
    {
        var (pending, invitation) = await CreateInvitationAsync();
        var password = input == "too-long" ? new string('x', 1025) : input;
        var hasher = new CallbackHasher((_, _) => throw new Xunit.Sdk.XunitException("Hashing invalid input."));
        var service = new AccountInvitationService(Repository, Repository, hasher, _clock);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RedeemAsync(invitation.Token, password!));
        Assert.Equal(0, hasher.Calls);
        await AssertUntouchedAsync(pending, invitation);
        await Service.RedeemAsync(invitation.Token, Password);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("malformed")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("not-yet-created")]
    [InlineData("inactive")]
    [InlineData("suspended")]
    [InlineData("active")]
    public async Task Ineligible_acceptance_is_rejected_before_password_hashing(string state)
    {
        var (pending, invitation) = await CreateInvitationAsync();
        var token = invitation.Token;
        switch (state)
        {
            case "unknown":
                token = AccountInvitationToken.Generate();
                break;
            case "malformed":
                token = "synthetic-invalid-token";
                break;
            case "expired":
                _clock.Advance(TimeSpan.FromHours(1));
                break;
            case "revoked":
                await Service.RevokeAsync(invitation.Id);
                break;
            case "not-yet-created":
                _clock.Advance(TimeSpan.FromSeconds(-1));
                break;
            case "inactive":
                await SetUserInactiveAsync(pending.UserId);
                break;
            case "suspended":
                await Accounts.SuspendAsync(pending.Id);
                break;
            case "active":
                await Accounts.ActivateAsync(pending.Id, Password);
                break;
        }
        var originalAccount = await Repository.GetAsync(pending.Id, default);
        var originalToken = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(invitation.Token), default);
        var originalAudit = await Repository.GetForAccountAsync(pending.Id, default);
        var hasher = new CallbackHasher((_, _) => throw new Xunit.Sdk.XunitException("Hashing ineligible input."));
        var service = new AccountInvitationService(Repository, Repository, hasher, _clock);

        if (state == "malformed")
            await Assert.ThrowsAsync<ArgumentException>(() => service.RedeemAsync(token, Password));
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemAsync(token, Password));

        Assert.Equal(0, hasher.Calls);
        Assert.Equal(originalAccount, await Repository.GetAsync(pending.Id, default));
        Assert.Equal(originalToken,
            await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(invitation.Token), default));
        Assert.Equal(originalAudit, await Repository.GetForAccountAsync(pending.Id, default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Hashing_failure_or_empty_hash_cannot_partially_activate(bool emptyHash)
    {
        var (pending, invitation) = await CreateInvitationAsync();
        var hasher = new CallbackHasher((_, _) => emptyHash
            ? ""
            : throw new IOException("Synthetic hashing failure."));
        var service = new AccountInvitationService(Repository, Repository, hasher, _clock);
        if (emptyHash)
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemAsync(invitation.Token, Password));
        else
            await Assert.ThrowsAsync<IOException>(() => service.RedeemAsync(invitation.Token, Password));
        Assert.Equal(1, hasher.Calls);
        await AssertUntouchedAsync(pending, invitation);
        await Service.RedeemAsync(invitation.Token, Password);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_before_or_during_hashing_prevents_acceptance(bool duringHash)
    {
        var (pending, invitation) = await CreateInvitationAsync();
        using var cancellation = new CancellationTokenSource();
        var hasher = new CallbackHasher((account, password) =>
        {
            var hash = _hasher.HashPassword(account, password);
            cancellation.Cancel();
            return hash;
        });
        if (!duringHash)
            cancellation.Cancel();
        var service = new AccountInvitationService(Repository, Repository, hasher, _clock);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RedeemAsync(invitation.Token, Password, cancellation.Token));
        Assert.Equal(duringHash ? 1 : 0, hasher.Calls);
        await AssertUntouchedAsync(pending, invitation);
        await Service.RedeemAsync(invitation.Token, Password);
    }

    [Fact]
    public async Task Expiration_during_hashing_fails_without_consuming_the_invitation()
    {
        var (pending, invitation) = await CreateInvitationAsync();
        var hasher = new CallbackHasher((account, password) =>
        {
            var hash = _hasher.HashPassword(account, password);
            _clock.Advance(TimeSpan.FromHours(1));
            return hash;
        });
        var service = new AccountInvitationService(Repository, Repository, hasher, _clock);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemAsync(invitation.Token, Password));
        Assert.Equal(1, hasher.Calls);
        await AssertUntouchedAsync(pending, invitation);
    }

    [Fact]
    public async Task Account_write_failure_rolls_back_token_consumption_and_allows_retry()
    {
        var (pending, invitation) = await CreateInvitationAsync();
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync($"""
            ALTER TABLE "Accounts" ADD CONSTRAINT "SyntheticAcceptanceFailure"
            CHECK ("Id" <> '{pending.Id}'::uuid OR "Status" <> 'Active') NOT VALID
            """);
        try
        {
            await Assert.ThrowsAsync<PostgresException>(() => Service.RedeemAsync(invitation.Token, Password));
            await AssertUntouchedAsync(pending, invitation);
        }
        finally
        {
            await connection.ExecuteAsync("""ALTER TABLE "Accounts" DROP CONSTRAINT "SyntheticAcceptanceFailure" """);
        }

        var accepted = await Service.RedeemAsync(invitation.Token, Password);
        Assert.Equal(AccountStatus.Active, accepted.Status);
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(accepted, accepted.PasswordHash!, Password));
        Assert.Single(await Repository.GetForAccountAsync(pending.Id, default),
            entry => entry.EventType == "InvitationRedeemed");
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("suspended")]
    [InlineData("revoked")]
    [InlineData("version-conflict")]
    public async Task Eligibility_changes_after_hashing_are_rechecked_transactionally(string change)
    {
        var (pending, invitation) = await CreateInvitationAsync();
        var intercept = new BeforeRedemptionRepository(Repository, async () =>
        {
            switch (change)
            {
                case "inactive":
                    await SetUserInactiveAsync(pending.UserId);
                    break;
                case "suspended":
                    await Accounts.SuspendAsync(pending.Id);
                    break;
                case "revoked":
                    await Service.RevokeAsync(invitation.Id);
                    break;
                case "version-conflict":
                    Assert.True(await Repository.TryUpdateAsync(pending with { Version = 1 }, 0, default));
                    break;
            }
        });
        var service = new AccountInvitationService(Repository, intercept, _hasher, _clock);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemAsync(invitation.Token, Password));

        var account = await Repository.GetAsync(pending.Id, default);
        Assert.Null(account!.PasswordHash);
        Assert.Equal(change == "suspended" ? AccountStatus.Suspended : AccountStatus.Pending, account.Status);
        var token = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(invitation.Token), default);
        Assert.Null(token!.RedeemedAt);
        Assert.Equal(change == "revoked", token.RevokedAt is not null);
        Assert.DoesNotContain(await Repository.GetForAccountAsync(pending.Id, default),
            entry => entry.EventType == "InvitationRedeemed");
        if (change == "version-conflict")
            Assert.Equal(2, (await Service.RedeemAsync(invitation.Token, Password)).Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Overlapping_acceptances_commit_only_the_winners_password(bool siblingTokens)
    {
        var (pending, first) = await CreateInvitationAsync();
        var second = siblingTokens ? await Service.IssueAsync(pending.Id, TimeSpan.FromHours(1)) : first;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var intercept = new BeforeRedemptionRepository(Repository, async () =>
        {
            if (Interlocked.Increment(ref attempts) == 2)
                ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        });
        var service = new AccountInvitationService(Repository, intercept, _hasher, _clock);
        var passwords = new[] { Password, Password + "-second" };
        var results = await Task.WhenAll(
            AcceptOrRejectAsync(service, first.Token, passwords[0]),
            AcceptOrRejectAsync(service, second.Token, passwords[1]));

        Assert.Equal(2, attempts);
        var winner = Assert.Single(results, account => account is not null);
        var winnerIndex = results[0] is not null ? 0 : 1;
        var stored = await Repository.GetAsync(pending.Id, default);
        Assert.Equal(winner, stored);
        Assert.Equal(AccountStatus.Active, stored!.Status);
        Assert.Equal(1, stored.Version);
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(stored, stored.PasswordHash!, passwords[winnerIndex]));
        Assert.Equal(PasswordVerificationResult.Failed,
            _hasher.VerifyHashedPassword(stored, stored.PasswordHash!, passwords[1 - winnerIndex]));
        var audit = await Repository.GetForAccountAsync(pending.Id, default);
        Assert.Single(audit, entry => entry.EventType == "InvitationRedeemed");
        await using var connection = await database.DataSource.OpenConnectionAsync();
        Assert.Equal(1, await connection.QuerySingleAsync<int>(
            """SELECT count(*)::int FROM "AccountInvitations" WHERE "AccountId" = @Id AND "RedeemedAt" IS NOT NULL""",
            new { pending.Id }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RedeemAsync(first.Token, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RedeemAsync(second.Token, Password));
        Assert.Equal(stored, await Repository.GetAsync(pending.Id, default));
        Assert.Equal(audit, await Repository.GetForAccountAsync(pending.Id, default));
    }

    private AccountService Accounts => new(Repository, _hasher, _clock);

    private async Task<(Account Account, IssuedAccountInvitation Invitation)> CreateInvitationAsync()
    {
        var userId = Guid.NewGuid();
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO "Users" ("Id", "Name", "Email", "Role")
            VALUES (@userId, 'Synthetic Acceptance', @email, 'Operational label only')
            """, new { userId, email = $"{userId:N}@example.invalid" });
        var account = await Accounts.CreatePendingAsync(userId);
        return (account, await Service.IssueAsync(account.Id, TimeSpan.FromHours(1)));
    }

    private async Task SetUserInactiveAsync(Guid userId)
    {
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("""UPDATE "Users" SET "IsActive" = false WHERE "Id" = @userId""", new { userId });
    }

    private async Task AssertUntouchedAsync(Account account, IssuedAccountInvitation invitation)
    {
        Assert.Equal(account, await Repository.GetAsync(account.Id, default));
        var stored = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(invitation.Token), default);
        Assert.Null(stored!.RedeemedAt);
        Assert.Null(stored.RevokedAt);
        Assert.Equal(2, (await Repository.GetForAccountAsync(account.Id, default)).Count);
    }

    private static async Task<Account?> AcceptOrRejectAsync(
        AccountInvitationService service, string token, string password)
    {
        try
        {
            return await service.RedeemAsync(token, password);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private sealed class CallbackHasher(Func<Account, string, string> hash) : IPasswordHasher<Account>
    {
        public int Calls { get; private set; }
        public string HashPassword(Account user, string password)
        {
            Calls++;
            return hash(user, password);
        }

        public PasswordVerificationResult VerifyHashedPassword(Account user, string hashedPassword, string providedPassword) =>
            throw new NotSupportedException();
    }

    private sealed class BeforeRedemptionRepository(
        IAccountInvitationRepository inner, Func<Task> beforeRedemption) : IAccountInvitationRepository
    {
        public Task<AccountInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            inner.GetByTokenHashAsync(tokenHash, cancellationToken);
        public Task<bool> TryIssueAsync(AccountInvitation invitation, CancellationToken cancellationToken) =>
            inner.TryIssueAsync(invitation, cancellationToken);
        public Task<bool> TryRevokeAsync(Guid invitationId, DateTimeOffset now, CancellationToken cancellationToken) =>
            inner.TryRevokeAsync(invitationId, now, cancellationToken);

        public async Task<bool> TryRedeemAsync(
            AccountInvitation invitation, Account activatedAccount, long expectedVersion,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            await beforeRedemption();
            return await inner.TryRedeemAsync(invitation, activatedAccount, expectedVersion, now, cancellationToken);
        }
    }
}
