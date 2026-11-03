using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace EventHelm.Api.Tests;

public sealed class IndividualAuthenticationTests(AccountPostgresFixture database) : IClassFixture<AccountPostgresFixture>
{
    private const string Password = "synthetic-individual-passphrase";
    private readonly TestClock _clock = new();
    private readonly AccountPasswordHasher _hasher = new();
    private AccountRepository Accounts => new(database.DataSource);
    private AccountSessionRepository Repository => new(database.DataSource);
    private AccountService Lifecycle => new(Accounts, _hasher, _clock);
    private IndividualAuthenticationService Authentication => new(Accounts, Repository, _hasher, _clock);
    private AccountSessionService Sessions => new(Repository, _clock);

    [Fact]
    public async Task Login_uses_explicit_identifier_not_profile_and_persists_only_random_token_hash()
    {
        var (account, email, profileEmail) = await CreateAccountAsync();
        var authentication = Authentication;
        Assert.Null(await authentication.AuthenticateAsync(profileEmail, Password));
        var issued = await authentication.AuthenticateAsync(" " + email.ToUpperInvariant() + " ", Password);
        Assert.NotNull(issued);
        var second = await authentication.AuthenticateAsync(email, Password);
        Assert.NotNull(second);
        Assert.NotEqual(issued.Token, second.Token);
        Assert.Equal(32, Convert.FromHexString(issued.Token).Length);
        Assert.Equal(_clock.GetUtcNow().AddHours(8), issued.ExpiresAt);
        var valid = await Sessions.ValidateAsync(issued.Token);
        Assert.Equal(account.Id, valid!.AccountId);
        Assert.Equal(account.UserId, valid.UserId);
        Assert.Equal(issued.ExpiresAt, valid.ExpiresAt);
        Assert.DoesNotContain(issued.Token, JsonSerializer.Serialize(issued));
        Assert.DoesNotContain(issued.Token, issued.ToString());
        Assert.DoesNotContain(Password, JsonSerializer.Serialize(valid));
        await using var connection = await database.DataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<string>(
            """SELECT row_to_json(s)::text FROM "AccountSessions" s WHERE "AccountId" = @Id""", new { account.Id });
        Assert.All(rows, row =>
        {
            Assert.DoesNotContain(issued.Token, row);
            Assert.DoesNotContain(second.Token, row);
            Assert.DoesNotContain(Password, row);
            Assert.DoesNotContain(account.PasswordHash!, row);
        });
        Assert.Equal(account, await Accounts.GetAsync(account.Id, default));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("wrong-password")]
    [InlineData("pending")]
    [InlineData("suspended")]
    [InlineData("inactive")]
    [InlineData("unbound")]
    public async Task All_credential_and_eligibility_failures_have_the_same_result(string reason)
    {
        var (account, email, _) = await CreateAccountAsync(reason != "pending", reason != "unbound");
        if (reason == "suspended")
            await Lifecycle.SuspendAsync(account.Id);
        if (reason == "inactive")
            await SetInactiveAsync(account.UserId);
        var result = await Authentication.AuthenticateAsync(
            reason == "unknown" ? $"unknown-{Guid.NewGuid():N}@example.invalid" : email,
            reason == "wrong-password" ? "wrong-password" : Password);
        Assert.Null(result);
        Assert.Equal(0, await SessionCountAsync(account.Id));
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("not an email", "password")]
    [InlineData("Display <a@example.invalid>", "password")]
    [InlineData("a@example.invalid", null)]
    [InlineData("a@example.invalid", "")]
    public async Task Invalid_input_is_generic_and_never_issues_session(string? email, string? password)
    {
        Assert.Null(await Authentication.AuthenticateAsync(email, password));
    }

    [Fact]
    public async Task Binding_is_explicit_unique_and_immutable_and_rejects_noncanonical_email()
    {
        var (account, email, _) = await CreateAccountAsync();
        var (other, _, _) = await CreateAccountAsync(bind: false);
        await Assert.ThrowsAsync<PostgresException>(() => Repository.BindLoginEmailAsync(other.Id, email, default));
        await Assert.ThrowsAsync<PostgresException>(() =>
            Repository.BindLoginEmailAsync(account.Id, $"new-{Guid.NewGuid():N}@example.invalid", default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Repository.BindLoginEmailAsync(other.Id, email.ToUpperInvariant(), default));
        Assert.Null(AccountLoginEmail.Normalize("person\u00e9@example.invalid"));
        Assert.Null(await Authentication.AuthenticateAsync(email, new string('x', 1025)));
    }

    [Fact]
    public async Task Five_failed_attempts_lock_out_even_correct_password_then_recover_after_expiry()
    {
        var (account, email, _) = await CreateAccountAsync();
        var authentication = Authentication;
        for (var index = 0; index < 5; index++)
            Assert.Null(await authentication.AuthenticateAsync(email, "wrong-password"));
        Assert.Null(await authentication.AuthenticateAsync(email.ToUpperInvariant(), Password));
        Assert.Equal(0, await SessionCountAsync(account.Id));
        _clock.Advance(TimeSpan.FromMinutes(15));
        Assert.NotNull(await authentication.AuthenticateAsync(email, Password));
    }

    [Fact]
    public async Task Attempt_budget_is_persistent_and_atomic_for_unknown_identifiers()
    {
        var identifier = Hash($"synthetic-{Guid.NewGuid():N}@example.invalid");
        var attempts = await Task.WhenAll(Enumerable.Range(0, 30).Select(_ =>
            Repository.TryReserveAttemptAsync(identifier, _clock.GetUtcNow(), default)));
        Assert.Equal(10, attempts.Count(result => result));
        Assert.False(await new AccountSessionRepository(database.DataSource)
            .TryReserveAttemptAsync(identifier, _clock.GetUtcNow(), default));
        _clock.Advance(TimeSpan.FromMinutes(15));
        Assert.True(await Repository.TryReserveAttemptAsync(identifier, _clock.GetUtcNow(), default));
    }

    [Fact]
    public async Task Successful_logins_also_obey_the_attempt_budget()
    {
        var (_, email, _) = await CreateAccountAsync();
        var authentication = Authentication;
        for (var index = 0; index < 10; index++)
            Assert.NotNull(await authentication.AuthenticateAsync(email, Password));
        Assert.Null(await authentication.AuthenticateAsync(email, Password));
    }

    [Fact]
    public async Task Session_expiration_is_exclusive_and_unknown_or_malformed_tokens_fail()
    {
        var (_, email, _) = await CreateAccountAsync();
        var issued = (await Authentication.AuthenticateAsync(email, Password))!;
        _clock.Advance(TimeSpan.FromHours(8) - TimeSpan.FromSeconds(1));
        Assert.NotNull(await Sessions.ValidateAsync(issued.Token));
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(await Sessions.ValidateAsync(issued.Token));
        Assert.Null(await Sessions.ValidateAsync(null));
        Assert.Null(await Sessions.ValidateAsync("invalid"));
        Assert.Null(await Sessions.ValidateAsync(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))));
    }

    [Fact]
    public async Task Database_clock_prevents_stale_application_time_extending_a_session()
    {
        var (_, email, _) = await CreateAccountAsync();
        var issued = (await Authentication.AuthenticateAsync(email, Password))!;
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            UPDATE "AccountSessions" SET "CreatedAt" = clock_timestamp() - interval '1 hour',
                "ExpiresAt" = clock_timestamp() - interval '1 second' WHERE "TokenHash" = @hash
            """, new { hash = Hash(issued.Token) });
        Assert.Null(await Repository.ValidateAsync(Hash(issued.Token), DateTimeOffset.UtcNow.AddMinutes(-2), default));
    }

    [Fact]
    public async Task Single_revocation_is_idempotent_concurrent_and_does_not_revoke_other_sessions()
    {
        var (_, email, _) = await CreateAccountAsync();
        var authentication = Authentication;
        var first = (await authentication.AuthenticateAsync(email, Password))!;
        var second = (await authentication.AuthenticateAsync(email, Password))!;
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Sessions.RevokeAsync(first.Token)));
        Assert.Null(await Sessions.ValidateAsync(first.Token));
        Assert.NotNull(await Sessions.ValidateAsync(second.Token));
    }

    [Theory]
    [InlineData("suspend")]
    [InlineData("password")]
    [InlineData("account-version")]
    [InlineData("security-version")]
    [InlineData("inactive")]
    public async Task Account_security_changes_invalidate_existing_sessions(string change)
    {
        var (account, email, _) = await CreateAccountAsync();
        var issued = (await Authentication.AuthenticateAsync(email, Password))!;
        switch (change)
        {
            case "suspend":
                await Lifecycle.SuspendAsync(account.Id);
                break;
            case "password":
                Assert.True(await Accounts.TryUpdateAsync(account with
                {
                    PasswordHash = _hasher.HashPassword(account, Password + "-changed"),
                    Version = account.Version + 1
                }, account.Version, default));
                break;
            case "account-version":
                Assert.True(await Accounts.TryUpdateAsync(account with { Version = account.Version + 1 },
                    account.Version, default));
                break;
            case "security-version":
                await using (var connection = await database.DataSource.OpenConnectionAsync())
                    await connection.ExecuteAsync(
                        """UPDATE "AccountSecurity" SET "SecurityVersion" = "SecurityVersion" + 1 WHERE "AccountId" = @Id""",
                        new { account.Id });
                break;
            case "inactive":
                await SetInactiveAsync(account.UserId);
                break;
        }
        Assert.Null(await Sessions.ValidateAsync(issued.Token));
    }

    [Fact]
    public async Task Concurrent_logout_all_invalidates_every_session_and_allows_only_fresh_logins()
    {
        var (account, email, _) = await CreateAccountAsync();
        var authentication = Authentication;
        var first = (await authentication.AuthenticateAsync(email, Password))!;
        var second = (await authentication.AuthenticateAsync(email, Password))!;
        var captured = (await Repository.FindLoginAsync(email, default))!;
        await Task.WhenAll(Sessions.RevokeAllAsync(account.Id), Sessions.RevokeAllAsync(account.Id));
        Assert.Null(await Sessions.ValidateAsync(first.Token));
        Assert.Null(await Sessions.ValidateAsync(second.Token));
        Assert.False(await Repository.TryCreateSessionAsync(captured, Hash(email), Hash("synthetic-stale-session"),
            _clock.GetUtcNow(), _clock.GetUtcNow().AddHours(8), default));
        var fresh = (await authentication.AuthenticateAsync(email, Password))!;
        Assert.NotNull(await Sessions.ValidateAsync(fresh.Token));
    }

    [Fact]
    public async Task Session_issuance_racing_logout_all_cannot_leave_a_valid_old_generation_session()
    {
        var (account, email, _) = await CreateAccountAsync();
        var identifier = Hash(email);
        Assert.True(await Repository.TryReserveAttemptAsync(identifier, _clock.GetUtcNow(), default));
        var captured = (await Repository.FindLoginAsync(email, default))!;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var issuance = Repository.TryCreateSessionAsync(captured, identifier, Hash(token),
            _clock.GetUtcNow(), _clock.GetUtcNow().AddHours(8), default);
        await Task.WhenAll(issuance, Sessions.RevokeAllAsync(account.Id));
        Assert.Null(await Sessions.ValidateAsync(token));
        var fresh = (await Authentication.AuthenticateAsync(email, Password))!;
        Assert.NotNull(await Sessions.ValidateAsync(fresh.Token));
    }

    [Fact]
    public async Task Logout_all_failure_rolls_back_both_revocations_and_security_version()
    {
        var (account, email, _) = await CreateAccountAsync();
        var issued = (await Authentication.AuthenticateAsync(email, Password))!;
        var before = (await Repository.FindLoginAsync(email, default))!;
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync($"""
            ALTER TABLE "AccountSessions" ADD CONSTRAINT "SyntheticRevocationFailure"
            CHECK ("AccountId" <> '{account.Id}'::uuid OR "RevokedAt" IS NULL) NOT VALID
            """);
        try
        {
            await Assert.ThrowsAsync<PostgresException>(() => Sessions.RevokeAllAsync(account.Id));
            Assert.Equal(before.SecurityVersion,
                (await Repository.FindLoginAsync(email, default))!.SecurityVersion);
            Assert.NotNull(await Sessions.ValidateAsync(issued.Token));
        }
        finally
        {
            await connection.ExecuteAsync("""ALTER TABLE "AccountSessions" DROP CONSTRAINT "SyntheticRevocationFailure" """);
        }
        await Sessions.RevokeAllAsync(account.Id);
        Assert.Null(await Sessions.ValidateAsync(issued.Token));
    }

    [Fact]
    public async Task Login_lookup_does_not_hold_a_second_pool_connection()
    {
        var (_, email, _) = await CreateAccountAsync();
        await using var singleConnectionSource = database.CreateSingleConnectionDataSource();
        Assert.NotNull(await new AccountSessionRepository(singleConnectionSource).FindLoginAsync(email, default));
    }

    [Theory]
    [InlineData("suspend")]
    [InlineData("inactive")]
    [InlineData("version")]
    [InlineData("lockout")]
    public async Task Session_issuance_rechecks_state_after_credential_verification(string change)
    {
        var (account, email, _) = await CreateAccountAsync();
        var identifier = Hash(email);
        Assert.True(await Repository.TryReserveAttemptAsync(identifier, _clock.GetUtcNow(), default));
        var captured = (await Repository.FindLoginAsync(email, default))!;
        if (change == "suspend")
            await Lifecycle.SuspendAsync(account.Id);
        else if (change == "inactive")
            await SetInactiveAsync(account.UserId);
        else if (change == "version")
            Assert.True(await Accounts.TryUpdateAsync(account with { Version = account.Version + 1 }, account.Version, default));
        else
            for (var index = 0; index < 5; index++)
                await Repository.RecordFailureAsync(identifier, _clock.GetUtcNow(), default);
        Assert.False(await Repository.TryCreateSessionAsync(captured, identifier, Hash("synthetic-candidate"),
            _clock.GetUtcNow(), _clock.GetUtcNow().AddHours(8), default));
        Assert.Equal(0, await SessionCountAsync(account.Id));
    }

    [Fact]
    public async Task Identity_rehash_advances_version_and_invalidates_old_sessions()
    {
        var (account, email, _) = await CreateAccountAsync();
        var olderHasher = new PasswordHasher<Account>(Options.Create(new PasswordHasherOptions { IterationCount = 10_000 }));
        account = account with
        {
            PasswordHash = olderHasher.HashPassword(account, Password),
            Version = account.Version + 1
        };
        Assert.True(await Accounts.TryUpdateAsync(account, account.Version - 1, default));
        var identifier = Hash(email);
        Assert.True(await Repository.TryReserveAttemptAsync(identifier, _clock.GetUtcNow(), default));
        var captured = (await Repository.FindLoginAsync(email, default))!;
        var oldToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        Assert.True(await Repository.TryCreateSessionAsync(captured, identifier, Hash(oldToken),
            _clock.GetUtcNow(), _clock.GetUtcNow().AddHours(8), default));
        var fresh = (await Authentication.AuthenticateAsync(email, Password))!;
        var updated = (await Accounts.GetAsync(account.Id, default))!;
        Assert.Equal(account.Version + 1, updated.Version);
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(updated, updated.PasswordHash!, Password));
        Assert.Null(await Sessions.ValidateAsync(oldToken));
        Assert.NotNull(await Sessions.ValidateAsync(fresh.Token));
    }

    [Fact]
    public async Task Storage_unavailability_never_returns_a_valid_session_or_successful_login()
    {
        var (_, email, _) = await CreateAccountAsync();
        var authentication = Authentication;
        var issued = (await authentication.AuthenticateAsync(email, Password))!;
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("""ALTER TABLE "AccountSessions" RENAME TO "SyntheticUnavailableSessions" """);
        try
        {
            await Assert.ThrowsAsync<PostgresException>(() => Sessions.ValidateAsync(issued.Token));
            await Assert.ThrowsAsync<PostgresException>(() => authentication.AuthenticateAsync(email, Password));
        }
        finally
        {
            await connection.ExecuteAsync("""ALTER TABLE "SyntheticUnavailableSessions" RENAME TO "AccountSessions" """);
        }
        Assert.NotNull(await Sessions.ValidateAsync(issued.Token));
    }

    [Fact]
    public async Task Cancellation_propagates_without_creating_a_session()
    {
        var (account, email, _) = await CreateAccountAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Authentication.AuthenticateAsync(email, Password, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Sessions.ValidateAsync(new string('A', 64), cancellation.Token));
        Assert.Equal(0, await SessionCountAsync(account.Id));
    }

    private async Task<(Account Account, string Email, string ProfileEmail)> CreateAccountAsync(
        bool activate = true, bool bind = true)
    {
        var userId = Guid.NewGuid();
        var email = $"login-{userId:N}@example.invalid";
        var profileEmail = $"profile-{userId:N}@example.invalid";
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO "Users" ("Id", "Name", "Email", "Role")
            VALUES (@userId, 'Synthetic Session User', @profileEmail, 'Admin')
            """, new { userId, profileEmail });
        var account = await Lifecycle.CreatePendingAsync(userId);
        if (activate)
            account = await Lifecycle.ActivateAsync(account.Id, Password);
        if (bind)
            await Repository.BindLoginEmailAsync(account.Id, email, default);
        return (account, email, profileEmail);
    }

    private async Task<int> SessionCountAsync(Guid accountId)
    {
        await using var connection = await database.DataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<int>(
            """SELECT count(*)::int FROM "AccountSessions" WHERE "AccountId" = @accountId""", new { accountId });
    }

    private async Task SetInactiveAsync(Guid userId)
    {
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("""UPDATE "Users" SET "IsActive" = false WHERE "Id" = @userId""", new { userId });
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
