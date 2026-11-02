using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class AccountSessionRepository(NpgsqlDataSource dataSource) : IAccountSessionRepository
{
    public async Task BindLoginEmailAsync(Guid accountId, string normalizedEmail, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || AccountLoginEmail.Normalize(normalizedEmail) != normalizedEmail)
            throw new ArgumentException("A normalized login email and account UUID are required.");
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(Command(
            """INSERT INTO "AccountSecurity" ("AccountId", "LoginEmail") VALUES (@accountId, @normalizedEmail)""",
            new { accountId, normalizedEmail }, cancellationToken));
    }

    public async Task<bool> TryReserveAttemptAsync(
        string identifierHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(Command(
            """
            INSERT INTO "AccountLoginAttempts" ("IdentifierHash", "WindowStartedAt", "Attempts", "Failures")
            VALUES (@identifierHash, @now, 1, 0)
            ON CONFLICT ("IdentifierHash") DO UPDATE SET
                "Attempts" = CASE WHEN "AccountLoginAttempts"."WindowStartedAt" <= @now - interval '15 minutes'
                    THEN 1 ELSE "AccountLoginAttempts"."Attempts" + 1 END,
                "Failures" = CASE WHEN "AccountLoginAttempts"."WindowStartedAt" <= @now - interval '15 minutes'
                    THEN 0 ELSE "AccountLoginAttempts"."Failures" END,
                "WindowStartedAt" = CASE WHEN "AccountLoginAttempts"."WindowStartedAt" <= @now - interval '15 minutes'
                    THEN @now ELSE "AccountLoginAttempts"."WindowStartedAt" END
            WHERE ("AccountLoginAttempts"."LockedUntil" IS NULL OR "AccountLoginAttempts"."LockedUntil" <= @now)
                AND ("AccountLoginAttempts"."Attempts" < 10
                    OR "AccountLoginAttempts"."WindowStartedAt" <= @now - interval '15 minutes')
            """, new { identifierHash, now }, cancellationToken)) == 1;
    }

    public async Task RecordFailureAsync(
        string identifierHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteAsync(Command(
            """
            UPDATE "AccountLoginAttempts"
            SET "Failures" = "Failures" + 1,
                "LockedUntil" = CASE WHEN "Failures" + 1 >= 5
                    THEN @now + interval '15 minutes' ELSE "LockedUntil" END
            WHERE "IdentifierHash" = @identifierHash
            """, new { identifierHash, now }, cancellationToken));
        if (count != 1)
            throw new InvalidOperationException("The login attempt reservation is missing.");
    }

    public async Task<AccountLogin?> FindLoginAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        SecurityRow? row;
        await using (var connection = await dataSource.OpenConnectionAsync(cancellationToken))
        {
            row = await connection.QuerySingleOrDefaultAsync<SecurityRow>(Command(
                """SELECT "AccountId", "SecurityVersion" FROM "AccountSecurity" WHERE "LoginEmail" = @normalizedEmail""",
                new { normalizedEmail }, cancellationToken));
        }
        if (row is null)
            return null;
        var account = await new AccountRepository(dataSource).GetAsync(row.AccountId, cancellationToken);
        return account is null ? null : new(account, row.SecurityVersion);
    }

    public async Task<bool> TryCreateSessionAsync(
        AccountLogin login, string identifierHash, string tokenHash, DateTimeOffset now,
        DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        if (expiresAt <= now || expiresAt > now.AddHours(8))
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        // Same lock order for issuance and logout-all; the captured security version
        // prevents a verified login from resurrecting a session across logout-all.
        var eligible = await connection.QuerySingleOrDefaultAsync<Guid?>(Command(
            """
            SELECT a."Id" FROM "Accounts" a
            JOIN "Users" u ON u."Id" = a."UserId"
            JOIN "AccountSecurity" s ON s."AccountId" = a."Id"
            JOIN "AccountLoginAttempts" t ON t."IdentifierHash" = @identifierHash
            WHERE a."Id" = @Id AND a."Status" = 'Active' AND u."IsActive"
                AND a."Version" = @Version AND a."PasswordHash" = @PasswordHash
                AND s."SecurityVersion" = @SecurityVersion
                AND (t."LockedUntil" IS NULL OR t."LockedUntil" <= @now)
            FOR UPDATE OF a, u, s, t
            """, new
            {
                login.Account.Id,
                login.Account.Version,
                login.Account.PasswordHash,
                login.SecurityVersion,
                identifierHash,
                now
            }, cancellationToken, transaction));
        if (eligible is null)
            return false;
        var count = await connection.ExecuteAsync(Command(
            """
            INSERT INTO "AccountSessions"
                ("TokenHash", "AccountId", "AccountVersion", "SecurityVersion", "CreatedAt", "ExpiresAt")
            SELECT @tokenHash, @Id, @Version, @SecurityVersion, @now, @expiresAt
            WHERE @expiresAt > clock_timestamp()
            """, new
            {
                tokenHash,
                login.Account.Id,
                login.Account.Version,
                login.SecurityVersion,
                now,
                expiresAt
            }, cancellationToken, transaction));
        await transaction.CommitAsync(cancellationToken);
        return count == 1;
    }

    public async Task<ValidAccountSession?> ValidateAsync(
        string tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(Command(
            """
            SELECT a."Id" AS "AccountId", a."UserId", t."ExpiresAt"
            FROM "AccountSessions" t
            JOIN "Accounts" a ON a."Id" = t."AccountId"
            JOIN "Users" u ON u."Id" = a."UserId"
            JOIN "AccountSecurity" s ON s."AccountId" = a."Id"
            WHERE t."TokenHash" = @tokenHash AND t."RevokedAt" IS NULL
                AND t."CreatedAt" <= @now AND t."ExpiresAt" > @now
                AND t."ExpiresAt" > clock_timestamp()
                AND a."Status" = 'Active' AND a."PasswordHash" IS NOT NULL AND u."IsActive"
                AND t."AccountVersion" = a."Version" AND t."SecurityVersion" = s."SecurityVersion"
            """, new { tokenHash, now }, cancellationToken));
        return row is null ? null : new(row.AccountId, row.UserId, new DateTimeOffset(row.ExpiresAt));
    }

    public async Task RevokeAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(Command(
            """
            UPDATE "AccountSessions" SET "RevokedAt" = @now
            WHERE "TokenHash" = @tokenHash AND "RevokedAt" IS NULL
            """, new { tokenHash, now }, cancellationToken));
    }

    public async Task RevokeAllAsync(Guid accountId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(Command(
            """SELECT "Id" FROM "Accounts" WHERE "Id" = @accountId FOR UPDATE""",
            new { accountId }, cancellationToken, transaction));
        var count = await connection.ExecuteAsync(Command(
            """
            UPDATE "AccountSecurity" SET "SecurityVersion" = "SecurityVersion" + 1
            WHERE "AccountId" = @accountId
            """, new { accountId }, cancellationToken, transaction));
        if (count != 1)
            throw new KeyNotFoundException("The account login identity does not exist.");
        await connection.ExecuteAsync(Command(
            """
            UPDATE "AccountSessions" SET "RevokedAt" = @now
            WHERE "AccountId" = @accountId AND "RevokedAt" IS NULL
            """, new { accountId, now }, cancellationToken, transaction));
        await transaction.CommitAsync(cancellationToken);
    }

    private static CommandDefinition Command(
        string sql, object parameters, CancellationToken cancellationToken, NpgsqlTransaction? transaction = null) =>
        new(sql, parameters, transaction, cancellationToken: cancellationToken);

    private sealed class SecurityRow
    {
        public Guid AccountId { get; set; }
        public long SecurityVersion { get; set; }
    }

    private sealed class SessionRow
    {
        public Guid AccountId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
