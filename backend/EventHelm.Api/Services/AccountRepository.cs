using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

// The caller supplies and owns the data source. No configuration or secrets are read here.
public sealed class AccountRepository(NpgsqlDataSource dataSource) :
    IAccountRepository, IAccountInvitationRepository, IAccountAuditLog
{
    public async Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<User>(Command(
            """SELECT "Id", "Name", "Email", "Phone", "Role", "IsActive", "CreatedAt" FROM "Users" WHERE "Id" = @userId""",
            new { userId }, cancellationToken));
    }

    public async Task<Account?> GetAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AccountRow>(Command(
            """SELECT * FROM "Accounts" WHERE "Id" = @accountId""",
            new { accountId }, cancellationToken));
        return row?.ToAccount();
    }

    public async Task<bool> TryCreateAsync(Account account, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var count = await connection.ExecuteAsync(Command(
            """
            INSERT INTO "Accounts" ("Id", "UserId", "Status", "PasswordHash", "CreatedAt", "UpdatedAt", "Version")
            VALUES (@Id, @UserId, @Status, @PasswordHash, @CreatedAt, @UpdatedAt, @Version)
            ON CONFLICT ("UserId") DO NOTHING
            """, Parameters(account), cancellationToken, transaction));
        if (count == 0)
            return false;
        await AppendAuditAsync(connection, transaction, account.Id, null, "AccountCreated",
            account.CreatedAt, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryUpdateAsync(
        Account account, long expectedVersion, CancellationToken cancellationToken)
    {
        ValidateVersion(account, expectedVersion);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(Command(
            """
            UPDATE "Accounts"
            SET "Status" = @Status, "PasswordHash" = @PasswordHash,
                "UpdatedAt" = @UpdatedAt, "Version" = "Version" + 1
            WHERE "Id" = @Id AND "Version" = @expectedVersion
                AND "UserId" = @UserId AND "CreatedAt" = @CreatedAt
            """, Parameters(account, expectedVersion), cancellationToken)) == 1;
    }

    public async Task<AccountInvitation?> GetByTokenHashAsync(
        string tokenHash, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<InvitationRow>(Command(
            """SELECT * FROM "AccountInvitations" WHERE "TokenHash" = @tokenHash""",
            new { tokenHash }, cancellationToken));
        return row?.ToInvitation();
    }

    public async Task<bool> TryIssueAsync(AccountInvitation invitation, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await LockEligibleAccountAsync(connection, transaction, invitation.AccountId, cancellationToken))
            return false;
        var count = await connection.ExecuteAsync(Command(
            """
            INSERT INTO "AccountInvitations" ("Id", "AccountId", "TokenHash", "CreatedAt", "ExpiresAt")
            SELECT @Id, @AccountId, @TokenHash, @CreatedAt, @ExpiresAt
            WHERE @ExpiresAt > clock_timestamp()
            """, invitation, cancellationToken, transaction));
        if (count == 0)
            return false;
        await AppendAuditAsync(connection, transaction, invitation.AccountId, invitation.Id,
            "InvitationIssued", invitation.CreatedAt, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRevokeAsync(
        Guid invitationId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var accountId = await connection.QuerySingleOrDefaultAsync<Guid?>(Command(
            """SELECT "AccountId" FROM "AccountInvitations" WHERE "Id" = @invitationId""",
            new { invitationId }, cancellationToken, transaction));
        if (accountId is null)
            return false;

        // All invitation mutations acquire the account lock before touching the token row.
        await connection.ExecuteAsync(Command(
            """SELECT "Id" FROM "Accounts" WHERE "Id" = @accountId FOR UPDATE""",
            new { accountId }, cancellationToken, transaction));
        var count = await connection.ExecuteAsync(Command(
            """
            UPDATE "AccountInvitations" SET "RevokedAt" = @now
            WHERE "Id" = @invitationId AND "RedeemedAt" IS NULL AND "RevokedAt" IS NULL
            """, new { invitationId, now }, cancellationToken, transaction));
        if (count == 0)
            return false;
        await AppendAuditAsync(connection, transaction, accountId.Value, invitationId,
            "InvitationRevoked", now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRedeemAsync(
        AccountInvitation invitation, Account activatedAccount, long expectedVersion,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        ValidateVersion(activatedAccount, expectedVersion);
        if (activatedAccount.Id != invitation.AccountId ||
            activatedAccount.Status != AccountStatus.Active ||
            string.IsNullOrWhiteSpace(activatedAccount.PasswordHash))
            throw new ArgumentException("Redemption must activate the invitation's account.", nameof(activatedAccount));

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await LockEligibleAccountAsync(connection, transaction, invitation.AccountId, cancellationToken))
            return false;
        var consumed = await connection.ExecuteAsync(Command(
            """
            UPDATE "AccountInvitations" SET "RedeemedAt" = @now
            WHERE "Id" = @Id AND "AccountId" = @AccountId AND "TokenHash" = @TokenHash
                AND "RedeemedAt" IS NULL AND "RevokedAt" IS NULL
                AND "CreatedAt" <= @now AND "ExpiresAt" > @now
                AND "ExpiresAt" > clock_timestamp()
            """, new { invitation.Id, invitation.AccountId, invitation.TokenHash, now },
            cancellationToken, transaction));
        if (consumed == 0)
            return false;
        var updated = await connection.ExecuteAsync(Command(
            """
            UPDATE "Accounts"
            SET "Status" = @Status, "PasswordHash" = @PasswordHash,
                "UpdatedAt" = @UpdatedAt, "Version" = "Version" + 1
            WHERE "Id" = @Id AND "Version" = @expectedVersion
                AND "UserId" = @UserId AND "CreatedAt" = @CreatedAt AND "Status" = 'Pending'
            """, Parameters(activatedAccount, expectedVersion), cancellationToken, transaction));
        if (updated == 0)
            return false; // Disposing the uncommitted transaction restores the token.
        await AppendAuditAsync(connection, transaction, invitation.AccountId, invitation.Id,
            "InvitationRedeemed", now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AccountAuditEvent>> GetForAccountAsync(
        Guid accountId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AuditRow>(Command(
            """SELECT * FROM "AccountAuditEvents" WHERE "AccountId" = @accountId ORDER BY "OccurredAt", "Id" """,
            new { accountId }, cancellationToken));
        return rows.Select(row => new AccountAuditEvent(
            row.Id, row.AccountId, row.InvitationId, row.EventType, new DateTimeOffset(row.OccurredAt))).ToList();
    }

    private static async Task<bool> LockEligibleAccountAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleOrDefaultAsync<Guid?>(Command(
            """
            SELECT a."Id" FROM "Accounts" a JOIN "Users" u ON u."Id" = a."UserId"
            WHERE a."Id" = @accountId AND a."Status" = 'Pending' AND u."IsActive"
            FOR UPDATE OF a, u
            """, new { accountId }, cancellationToken, transaction)) is not null;

    private static Task AppendAuditAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId,
        Guid? invitationId, string eventType, DateTimeOffset occurredAt, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(Command(
            """
            INSERT INTO "AccountAuditEvents" ("Id", "AccountId", "InvitationId", "EventType", "OccurredAt")
            VALUES (@id, @accountId, @invitationId, @eventType, @occurredAt)
            """, new { id = Guid.NewGuid(), accountId, invitationId, eventType, occurredAt },
            cancellationToken, transaction));

    private static CommandDefinition Command(
        string sql, object parameters, CancellationToken cancellationToken, NpgsqlTransaction? transaction = null) =>
        new(sql, parameters, transaction, cancellationToken: cancellationToken);

    private static object Parameters(Account account, long? expectedVersion = null) => new
    {
        account.Id,
        account.UserId,
        Status = account.Status.ToString(),
        account.PasswordHash,
        account.CreatedAt,
        account.UpdatedAt,
        account.Version,
        expectedVersion
    };

    private static void ValidateVersion(Account account, long expectedVersion)
    {
        if (expectedVersion < 0 || account.Version != checked(expectedVersion + 1))
            throw new ArgumentException("Updates must advance the expected version by one.", nameof(account));
    }

    private sealed class AccountRow
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = "";
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Version { get; set; }

        public Account ToAccount() => new()
        {
            Id = Id,
            UserId = UserId,
            Status = Enum.Parse<AccountStatus>(Status),
            PasswordHash = PasswordHash,
            CreatedAt = new DateTimeOffset(CreatedAt),
            UpdatedAt = new DateTimeOffset(UpdatedAt),
            Version = Version
        };
    }

    private sealed class InvitationRow
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string TokenHash { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public AccountInvitation ToInvitation() => new()
        {
            Id = Id,
            AccountId = AccountId,
            TokenHash = TokenHash,
            CreatedAt = new DateTimeOffset(CreatedAt),
            ExpiresAt = new DateTimeOffset(ExpiresAt),
            RedeemedAt = RedeemedAt is { } redeemed ? new DateTimeOffset(redeemed) : null,
            RevokedAt = RevokedAt is { } revoked ? new DateTimeOffset(revoked) : null
        };
    }

    private sealed class AuditRow
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid? InvitationId { get; set; }
        public string EventType { get; set; } = "";
        public DateTime OccurredAt { get; set; }
    }
}
