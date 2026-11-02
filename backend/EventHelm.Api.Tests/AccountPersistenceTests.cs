using System.Text.Json;
using Dapper;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using EventHelm.Api.Services;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventHelm.Api.Tests;

// No external connection string is accepted: only this newly created container is used.
public sealed class AccountPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("synthetic_accounts")
        .WithUsername("synthetic")
        .WithPassword(Guid.NewGuid().ToString("N"))
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
        await using var connection = await DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            CREATE TABLE "Users" (
                "Id" uuid PRIMARY KEY, "Name" text NOT NULL, "Email" text NOT NULL UNIQUE,
                "Phone" text NULL, "Role" text NOT NULL DEFAULT 'User',
                "IsActive" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);
        foreach (var file in new[] { "2026-11-02_account_foundations.sql", "2026-11-02_account_invitations.sql" })
        {
            var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Migrations", file));
            await connection.ExecuteAsync(sql);
        }
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
            await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}

public sealed class AccountPersistenceTests(AccountPostgresFixture database) : IClassFixture<AccountPostgresFixture>
{
    private const string Password = "synthetic-invitation-passphrase";
    private readonly TestClock _clock = new();
    private readonly AccountPasswordHasher _hasher = new();
    private AccountRepository Repository => new(database.DataSource);
    private AccountService Accounts => new(Repository, _hasher, _clock);
    private AccountInvitationService Invitations => new(Repository, Repository, _hasher, _clock);

    [Fact]
    public async Task Persistence_round_trips_status_hash_timestamps_and_leaves_user_unchanged()
    {
        var user = await CreateUserAsync();
        var pending = await Accounts.CreatePendingAsync(user.Id);
        Assert.Equal(pending, await Repository.GetAsync(pending.Id, default));
        Assert.Null(await Repository.GetAsync(Guid.NewGuid(), default));
        Assert.Null(await Repository.GetUserAsync(Guid.NewGuid(), default));
        _clock.Advance(TimeSpan.FromMinutes(1));
        var active = await Accounts.ActivateAsync(pending.Id, Password);
        Assert.Equal(active, await Repository.GetAsync(active.Id, default));
        Assert.True(await Accounts.VerifyCredentialsAsync(active.Id, Password));
        var suspended = await Accounts.SuspendAsync(active.Id);
        Assert.Equal(suspended, await Repository.GetAsync(active.Id, default));
        var unchanged = await Repository.GetUserAsync(user.Id, default);
        Assert.Equal(JsonSerializer.Serialize(user), JsonSerializer.Serialize(unchanged));
        var audit = Assert.Single(await Repository.GetForAccountAsync(pending.Id, default));
        Assert.Equal("AccountCreated", audit.EventType);
        Assert.Null(audit.InvitationId);
    }

    [Fact]
    public async Task Concurrent_creation_and_updates_preserve_uniqueness_and_versions()
    {
        var user = await CreateUserAsync();
        var now = _clock.GetUtcNow();
        Account NewAccount() => new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        var first = NewAccount();
        var second = NewAccount();
        var created = await Task.WhenAll(
            Repository.TryCreateAsync(first, default), Repository.TryCreateAsync(second, default));
        Assert.Equal(1, created.Count(success => success));
        var original = created[0] ? first : second;
        var update = original with { Status = AccountStatus.Suspended, Version = 1 };
        var updated = await Task.WhenAll(
            Repository.TryUpdateAsync(update, 0, default), Repository.TryUpdateAsync(update, 0, default));
        Assert.Equal(1, updated.Count(success => success));
        Assert.Equal(update, await Repository.GetAsync(original.Id, default));
        Assert.Single(await Repository.GetForAccountAsync(original.Id, default));
    }

    [Fact]
    public async Task Schema_enforces_foreign_keys_status_password_and_immutable_identity_updates()
    {
        var pending = await CreatePendingAsync();
        await using var connection = await database.DataSource.OpenConnectionAsync();
        foreach (var sql in new[]
        {
            """UPDATE "Accounts" SET "Status" = 'Unknown' WHERE "Id" = @Id""",
            """UPDATE "Accounts" SET "Status" = 'Active' WHERE "Id" = @Id""",
            """UPDATE "Accounts" SET "PasswordHash" = 'synthetic-hash' WHERE "Id" = @Id""",
            """UPDATE "Accounts" SET "Version" = -1 WHERE "Id" = @Id""",
            """UPDATE "Accounts" SET "UpdatedAt" = "CreatedAt" - interval '1 second' WHERE "Id" = @Id"""
        })
            await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(sql, new { pending.Id }));
        await Assert.ThrowsAsync<PostgresException>(() =>
            Repository.TryCreateAsync(pending with { Id = Guid.NewGuid(), UserId = Guid.NewGuid() }, default));
        var changed = pending with { UserId = Guid.NewGuid(), Version = 1 };
        Assert.False(await Repository.TryUpdateAsync(changed, 0, default));
        await Assert.ThrowsAsync<ArgumentException>(() => Repository.TryUpdateAsync(pending, 0, default));
        Assert.Equal(pending, await Repository.GetAsync(pending.Id, default));
    }

    [Fact]
    public async Task Invitation_stores_only_hash_and_redemption_activates_once_with_audit()
    {
        var pending = await CreatePendingAsync();
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromDays(1));
        var hash = AccountInvitationToken.Hash(issued.Token);
        var stored = await Repository.GetByTokenHashAsync(hash, default);
        Assert.NotNull(stored);
        Assert.Equal(issued.Id, stored.Id);
        Assert.Equal(hash, stored.TokenHash);
        Assert.NotEqual(issued.Token, stored.TokenHash);
        Assert.Equal(issued.ExpiresAt, stored.ExpiresAt);
        Assert.DoesNotContain(issued.Token, JsonSerializer.Serialize(issued));
        Assert.DoesNotContain(issued.Token, issued.ToString());
        Assert.DoesNotContain(hash, JsonSerializer.Serialize(stored));
        Assert.DoesNotContain(hash, stored.ToString());
        await using (var connection = await database.DataSource.OpenConnectionAsync())
        {
            var persisted = await connection.QuerySingleAsync<string>(
                """SELECT row_to_json(i)::text FROM "AccountInvitations" i WHERE "Id" = @Id""", new { issued.Id });
            Assert.DoesNotContain(issued.Token, persisted);
        }
        _clock.Advance(TimeSpan.FromMinutes(1));
        var active = await Invitations.RedeemAsync(issued.Token, Password);
        Assert.Equal(AccountStatus.Active, active.Status);
        Assert.Equal(1, active.Version);
        Assert.Equal(active, await Repository.GetAsync(active.Id, default));
        Assert.True(await Accounts.VerifyCredentialsAsync(active.Id, Password));
        var redeemed = await Repository.GetByTokenHashAsync(hash, default);
        Assert.Equal(_clock.GetUtcNow(), redeemed!.RedeemedAt);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RedeemAsync(issued.Token, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RevokeAsync(issued.Id));
        var audit = await Repository.GetForAccountAsync(pending.Id, default);
        Assert.Equal(3, audit.Count);
        Assert.Single(audit, entry => entry.EventType == "AccountCreated");
        Assert.Single(audit, entry => entry.EventType == "InvitationIssued" && entry.InvitationId == issued.Id);
        Assert.Single(audit, entry => entry.EventType == "InvitationRedeemed" && entry.InvitationId == issued.Id);
        var auditJson = JsonSerializer.Serialize(audit);
        Assert.DoesNotContain(issued.Token, auditJson);
        Assert.DoesNotContain(hash, auditJson);
        Assert.DoesNotContain(active.PasswordHash!, auditJson);
        Assert.DoesNotContain(Password, auditJson);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Expiration_is_exclusive_and_failed_redemption_does_not_change_account(int secondsBeforeExpiry)
    {
        var pending = await CreatePendingAsync();
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1));
        _clock.Advance(TimeSpan.FromHours(1) - TimeSpan.FromSeconds(secondsBeforeExpiry));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RedeemAsync(issued.Token, Password));
        Assert.Equal(pending, await Repository.GetAsync(pending.Id, default));
        var stored = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(issued.Token), default);
        Assert.Null(stored!.RedeemedAt);
        Assert.Equal(2, (await Repository.GetForAccountAsync(pending.Id, default)).Count);
    }

    [Fact]
    public async Task Revocation_is_terminal_including_for_expired_invitations()
    {
        var pending = await CreatePendingAsync();
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1));
        _clock.Advance(TimeSpan.FromHours(2));
        await Invitations.RevokeAsync(issued.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RedeemAsync(issued.Token, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RevokeAsync(issued.Id));
        var stored = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(issued.Token), default);
        Assert.Equal(_clock.GetUtcNow(), stored!.RevokedAt);
        Assert.Null(stored.RedeemedAt);
        Assert.Equal(pending, await Repository.GetAsync(pending.Id, default));
        Assert.Single(await Repository.GetForAccountAsync(pending.Id, default),
            entry => entry.EventType == "InvitationRevoked");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_redemption_of_same_or_sibling_tokens_has_one_winner(bool siblingTokens)
    {
        var pending = await CreatePendingAsync();
        var first = await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1));
        var second = siblingTokens
            ? await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1))
            : first;
        var results = await Task.WhenAll(
            AttemptAsync(() => Invitations.RedeemAsync(first.Token, Password)),
            AttemptAsync(() => Invitations.RedeemAsync(second.Token, Password + "-other")));
        Assert.Equal(1, results.Count(success => success));
        var active = await Repository.GetAsync(pending.Id, default);
        Assert.Equal(AccountStatus.Active, active!.Status);
        Assert.Equal(1, active.Version);
        Assert.Single(await Repository.GetForAccountAsync(pending.Id, default),
            entry => entry.EventType == "InvitationRedeemed");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RedeemAsync(first.Token, Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RedeemAsync(second.Token, Password));
    }

    [Fact]
    public async Task Concurrent_revocation_and_redemption_produce_one_terminal_state()
    {
        var pending = await CreatePendingAsync();
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1));
        var results = await Task.WhenAll(
            AttemptAsync(() => Invitations.RedeemAsync(issued.Token, Password)),
            AttemptAsync(() => Invitations.RevokeAsync(issued.Id)));
        Assert.Equal(1, results.Count(success => success));
        var stored = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(issued.Token), default);
        Assert.NotEqual(stored!.RedeemedAt is null, stored.RevokedAt is null);
        Assert.Equal(stored.RedeemedAt is null ? AccountStatus.Pending : AccountStatus.Active,
            (await Repository.GetAsync(pending.Id, default))!.Status);
        Assert.Equal(3, (await Repository.GetForAccountAsync(pending.Id, default)).Count);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("suspended")]
    [InlineData("active")]
    public async Task Issuance_and_redemption_recheck_account_and_user_state(string state)
    {
        var pending = await CreatePendingAsync();
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1));
        if (state == "inactive")
        {
            await using var connection = await database.DataSource.OpenConnectionAsync();
            await connection.ExecuteAsync("""UPDATE "Users" SET "IsActive" = false WHERE "Id" = @UserId""",
                new { pending.UserId });
        }
        else if (state == "suspended")
            await Accounts.SuspendAsync(pending.Id);
        else
            await Accounts.ActivateAsync(pending.Id, Password);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RedeemAsync(issued.Token, Password));
        var stored = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(issued.Token), default);
        Assert.Null(stored!.RedeemedAt);
        Assert.Equal(2, (await Repository.GetForAccountAsync(pending.Id, default)).Count);
    }

    [Fact]
    public async Task Stale_version_rolls_back_consumption_and_token_can_be_retried()
    {
        var pending = await CreatePendingAsync();
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1));
        var invitation = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(issued.Token), default);
        var candidate = pending with
        {
            Status = AccountStatus.Active,
            PasswordHash = _hasher.HashPassword(pending, Password),
            Version = 1
        };
        Assert.True(await Repository.TryUpdateAsync(pending with { Version = 1 }, 0, default));
        Assert.False(await Repository.TryRedeemAsync(invitation!, candidate, 0, _clock.GetUtcNow(), default));
        Assert.Null((await Repository.GetByTokenHashAsync(invitation!.TokenHash, default))!.RedeemedAt);
        Assert.Equal(2, (await Invitations.RedeemAsync(issued.Token, Password)).Version);
    }

    [Fact]
    public async Task Database_clock_rejects_expired_token_even_with_stale_application_time()
    {
        var user = await CreateUserAsync();
        var now = DateTimeOffset.UtcNow;
        var pending = new Account
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now.AddHours(-1)
        };
        Assert.True(await Repository.TryCreateAsync(pending, default));
        var invitation = new AccountInvitation
        {
            Id = Guid.NewGuid(),
            AccountId = pending.Id,
            TokenHash = AccountInvitationToken.Hash(AccountInvitationToken.Generate()),
            CreatedAt = now.AddMinutes(-30),
            ExpiresAt = now.AddMinutes(30)
        };
        Assert.True(await Repository.TryIssueAsync(invitation, default));
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """UPDATE "AccountInvitations" SET "ExpiresAt" = @expires WHERE "Id" = @Id""",
            new { invitation.Id, expires = now.AddMinutes(-1) });
        var candidate = pending with
        {
            Status = AccountStatus.Active,
            PasswordHash = _hasher.HashPassword(pending, Password),
            UpdatedAt = now,
            Version = 1
        };
        Assert.False(await Repository.TryRedeemAsync(invitation, candidate, 0, now.AddMinutes(-2), default));
        Assert.Null((await Repository.GetByTokenHashAsync(invitation.TokenHash, default))!.RedeemedAt);
        Assert.Equal(AccountStatus.Pending, (await Repository.GetAsync(pending.Id, default))!.Status);
        Assert.Equal(2, (await Repository.GetForAccountAsync(pending.Id, default)).Count);
    }

    [Fact]
    public async Task Invitation_schema_rejects_invalid_hashes_lifetimes_and_terminal_states()
    {
        var pending = await CreatePendingAsync();
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromDays(7));
        await using var connection = await database.DataSource.OpenConnectionAsync();
        foreach (var sql in new[]
        {
            """UPDATE "AccountInvitations" SET "TokenHash" = 'plaintext' WHERE "Id" = @Id""",
            """UPDATE "AccountInvitations" SET "ExpiresAt" = "CreatedAt" WHERE "Id" = @Id""",
            """UPDATE "AccountInvitations" SET "ExpiresAt" = "CreatedAt" + interval '8 days' WHERE "Id" = @Id""",
            """UPDATE "AccountInvitations" SET "RedeemedAt" = "ExpiresAt" WHERE "Id" = @Id""",
            """UPDATE "AccountInvitations" SET "RevokedAt" = "CreatedAt" - interval '1 second' WHERE "Id" = @Id""",
            """UPDATE "AccountInvitations" SET "RedeemedAt" = "CreatedAt", "RevokedAt" = "CreatedAt" WHERE "Id" = @Id"""
        })
            await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(sql, new { issued.Id }));
        await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO "AccountInvitations" ("Id", "AccountId", "TokenHash", "CreatedAt", "ExpiresAt")
            SELECT @otherId, "AccountId", "TokenHash", "CreatedAt", "ExpiresAt"
            FROM "AccountInvitations" WHERE "Id" = @Id
            """, new { issued.Id, otherId = Guid.NewGuid() }));
        Assert.Equal(2, (await Repository.GetForAccountAsync(pending.Id, default)).Count);
    }

    [Theory]
    [InlineData("AccountCreated")]
    [InlineData("InvitationIssued")]
    [InlineData("InvitationRedeemed")]
    [InlineData("InvitationRevoked")]
    public async Task Audit_failure_rolls_back_the_entire_mutation(string eventType)
    {
        var user = await CreateUserAsync();
        var pending = eventType == "AccountCreated" ? null : await Accounts.CreatePendingAsync(user.Id);
        var issued = eventType is "InvitationRedeemed" or "InvitationRevoked"
            ? await Invitations.IssueAsync(pending!.Id, TimeSpan.FromHours(1)) : null;
        await using var connection = await database.DataSource.OpenConnectionAsync();
        // The constraint exists only in this disposable database and only for this test.
        await connection.ExecuteAsync($"""
            ALTER TABLE "AccountAuditEvents" ADD CONSTRAINT "SyntheticAuditFailure"
            CHECK ("EventType" <> '{eventType}') NOT VALID
            """);
        try
        {
            Task Mutate() => eventType switch
            {
                "AccountCreated" => Accounts.CreatePendingAsync(user.Id),
                "InvitationIssued" => Invitations.IssueAsync(pending!.Id, TimeSpan.FromHours(1)),
                "InvitationRedeemed" => Invitations.RedeemAsync(issued!.Token, Password),
                _ => Invitations.RevokeAsync(issued!.Id)
            };
            await Assert.ThrowsAsync<PostgresException>(Mutate);
        }
        finally
        {
            await connection.ExecuteAsync("""ALTER TABLE "AccountAuditEvents" DROP CONSTRAINT "SyntheticAuditFailure" """);
        }
        if (pending is null)
        {
            Assert.Equal(0, await connection.QuerySingleAsync<int>(
                """SELECT count(*)::int FROM "Accounts" WHERE "UserId" = @Id""", new { user.Id }));
        }
        else
        {
            Assert.Equal(pending, await Repository.GetAsync(pending.Id, default));
            if (issued is null)
                Assert.Equal(0, await connection.QuerySingleAsync<int>(
                    """SELECT count(*)::int FROM "AccountInvitations" WHERE "AccountId" = @Id""", new { pending.Id }));
            else
            {
                var stored = await Repository.GetByTokenHashAsync(AccountInvitationToken.Hash(issued.Token), default);
                Assert.Null(stored!.RedeemedAt);
                Assert.Null(stored.RevokedAt);
                await Invitations.RedeemAsync(issued.Token, Password);
            }
        }
    }

    [Fact]
    public async Task Invalid_input_missing_tokens_and_cancellation_have_no_side_effects()
    {
        var pending = await CreatePendingAsync();
        foreach (var lifetime in new[] { TimeSpan.Zero, TimeSpan.FromSeconds(-1), TimeSpan.FromDays(8) })
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Invitations.IssueAsync(pending.Id, lifetime));
        await Assert.ThrowsAsync<ArgumentException>(() => Invitations.IssueAsync(Guid.Empty, TimeSpan.FromHours(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Invitations.IssueAsync(Guid.NewGuid(), TimeSpan.FromHours(1)));
        await Assert.ThrowsAsync<ArgumentException>(() => Invitations.RevokeAsync(Guid.Empty));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invitations.RevokeAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Invitations.RedeemAsync(AccountInvitationToken.Generate(), Password));
        foreach (var malformed in new[] { "", "not-a-token", new string('G', 64), new string('A', 65) })
            await Assert.ThrowsAsync<ArgumentException>(() => Invitations.RedeemAsync(malformed, Password));
        var issued = await Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1));
        await Assert.ThrowsAsync<ArgumentException>(() => Invitations.RedeemAsync(issued.Token, "short"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Invitations.IssueAsync(pending.Id, TimeSpan.FromHours(1), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Invitations.RedeemAsync(issued.Token, Password, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Invitations.RevokeAsync(issued.Id, cancellation.Token));
        Assert.Equal(pending, await Repository.GetAsync(pending.Id, default));
        Assert.Equal(2, (await Repository.GetForAccountAsync(pending.Id, default)).Count);
    }

    [Fact]
    public void Tokens_are_256_bit_random_values_with_deterministic_hashes_and_no_normalization()
    {
        var tokens = Enumerable.Range(0, 1000).Select(_ => AccountInvitationToken.Generate()).ToArray();
        Assert.Equal(tokens.Length, tokens.Distinct().Count());
        Assert.All(tokens, token =>
        {
            Assert.Equal(32, Convert.FromHexString(token).Length);
            var hash = AccountInvitationToken.Hash(token);
            Assert.Equal(64, hash.Length);
            Assert.Equal(hash, AccountInvitationToken.Hash(token));
            Assert.NotEqual(token, hash);
        });
        Assert.Equal(
            "D53EDA7A637C99CC7FB566D96E9FA109BF15C478410A3F5EB4D4C4E26CD081F6",
            AccountInvitationToken.Hash(new string('A', 64)));
        Assert.Throws<ArgumentException>(() => AccountInvitationToken.Hash(new string('a', 64)));
        Assert.Throws<ArgumentException>(() => AccountInvitationToken.Hash(null!));
    }

    private async Task<User> CreateUserAsync()
    {
        var id = Guid.NewGuid();
        await using var connection = await database.DataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<User>(
            """
            INSERT INTO "Users" ("Id", "Name", "Email", "Role")
            VALUES (@id, 'Synthetic Person', @email, 'Operational label only') RETURNING *
            """, new { id, email = $"{id:N}@example.invalid" });
    }

    private async Task<Account> CreatePendingAsync() => await Accounts.CreatePendingAsync((await CreateUserAsync()).Id);

    private static async Task<bool> AttemptAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
