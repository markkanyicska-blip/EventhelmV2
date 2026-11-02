using Dapper;
using Npgsql;

namespace EventHelm.Api.Authorization;

// Read-only; provisioning and revocation are deliberately not exposed by this foundation.
public sealed class PostgresAuthorizationStore(NpgsqlDataSource dataSource) : IAuthorizationStore
{
    public async Task<AuthorizationSnapshot?> GetAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        // One consistent snapshot; never reuse an authorization result on a later request.
        await using var transaction = await connection.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        var active = await connection.QuerySingleOrDefaultAsync<bool?>(new CommandDefinition(
            """
            SELECT a."Status" = 'Active' AND a."PasswordHash" IS NOT NULL AND u."IsActive"
            FROM "Accounts" a JOIN "Users" u ON u."Id" = a."UserId" WHERE a."Id" = @accountId
            """, new { accountId }, transaction, cancellationToken: cancellationToken));
        if (active is null)
            return null;
        var globals = await connection.QueryAsync<GrantRow>(Command(
            """SELECT * FROM "GlobalRoleAssignments" WHERE "AccountId" = @accountId"""));
        var events = await connection.QueryAsync<GrantRow>(Command(
            """SELECT r.* FROM "EventRoleAssignments" r JOIN "Events" e ON e."Id" = r."EventId" WHERE r."AccountId" = @accountId"""));
        var sensitive = await connection.QueryAsync<GrantRow>(Command(
            """SELECT g.* FROM "SensitivePermissionGrants" g JOIN "Events" e ON e."Id" = g."EventId" WHERE g."AccountId" = @accountId"""));
        await transaction.CommitAsync(cancellationToken);
        return new(accountId, active.Value,
            globals.Select(r => new GlobalRoleAssignment(r.Id, r.AccountId, (GlobalRole)r.Role,
                r.GrantedAt, r.ExpiresAt, r.RevokedAt)).ToArray(),
            events.Select(r => new EventRoleAssignment(r.Id, r.AccountId, r.EventId, (EventRole)r.Role,
                r.GrantedAt, r.ExpiresAt, r.RevokedAt)).ToArray(),
            sensitive.Select(r => new SensitivePermissionGrant(r.Id, r.AccountId, r.EventId,
                (Permission)r.Permission, r.Purpose, r.GrantedAt,
                r.ExpiresAt ?? throw new InvalidOperationException("Sensitive grant expiry is missing."),
                r.RevokedAt)).ToArray());

        CommandDefinition Command(string sql) =>
            new(sql, new { accountId }, transaction, cancellationToken: cancellationToken);
    }

    private sealed class GrantRow
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid EventId { get; set; }
        public int Role { get; set; }
        public int Permission { get; set; }
        public string Purpose { get; set; } = "";
        public DateTimeOffset GrantedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
    }
}
