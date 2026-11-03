using Dapper;
using Npgsql;

namespace EventHelm.Api.Authorization;

// Only roots with authoritative scope are supported in this step. Other kinds deny
// until audited adapters can resolve classification, ownership and account assignments.
public sealed class PostgresAuthorizationResourceResolver(NpgsqlDataSource dataSource) : IAuthorizationResourceResolver
{
    public async Task<AuthorizationResource?> ResolveAsync(ResourceKey key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (key == AuthorizationResourceKeys.System || key == AuthorizationResourceKeys.EventCreation ||
            key == AuthorizationResourceKeys.GlobalInventory || key == AuthorizationResourceKeys.GlobalRaceControl)
            return new(key, ResourceScope.Global);
        if (key.Id == Guid.Empty || key.Kind is not (ResourceKind.Event or ResourceKind.EventRoles))
            return null;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            """SELECT EXISTS(SELECT 1 FROM "Events" WHERE "Id" = @Id)""",
            new { key.Id }, cancellationToken: cancellationToken));
        return exists ? new(key, ResourceScope.Event, key.Id) : null;
    }
}
