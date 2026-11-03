using Dapper;
using EventHelm.Api.Authorization;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using EventHelm.Api.Services;
using Npgsql;
using Xunit;

namespace EventHelm.Api.Tests;

// Wrap the established container fixture; never accept an external connection string,
// load appsettings, start Program, or read/execute deployment migration scripts.
public sealed class AuthorizationPostgresFixture : IAsyncLifetime
{
    private readonly AccountPostgresFixture _accounts = new();
    public NpgsqlDataSource DataSource => _accounts.DataSource;

    public async Task InitializeAsync()
    {
        await _accounts.InitializeAsync();
        await using var connection = await DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "rbac.sql")));
    }

    public Task DisposeAsync() => _accounts.DisposeAsync();
}

public sealed class AuthorizationPersistenceTests(AuthorizationPostgresFixture database)
    : IClassFixture<AuthorizationPostgresFixture>
{
    private const string Password = "synthetic-rbac-passphrase";
    private readonly TimeProvider _clock = TimeProvider.System;
    private PostgresAuthorizationStore Store => new(database.DataSource);
    private AccountSessionService Sessions => new(new AccountSessionRepository(database.DataSource), _clock);
    private EventAuthorizationService Service => new(
        Sessions, Store, new PostgresAuthorizationResourceResolver(database.DataSource), _clock);

    [Fact]
    public async Task Event_scope_is_resolved_server_side_and_legacy_role_text_grants_nothing()
    {
        var (account, token) = await CreateIdentityAsync();
        var first = await CreateEventAsync();
        var second = await CreateEventAsync();
        Assert.False((await Service.AuthorizeAsync(token, EventKey(first), ResourceAction.Read)).IsAllowed);
        await GrantRoleAsync(account.Id, first, EventRole.EventOrganizer);
        Assert.True((await Service.AuthorizeAsync(token, EventKey(first), ResourceAction.Manage)).IsAllowed);
        Assert.Equal(AuthorizationReason.MissingMembership,
            (await Service.AuthorizeAsync(token, EventKey(second), ResourceAction.Read)).Reason);
        Assert.Equal(AuthorizationReason.MissingResourceScope,
            (await Service.AuthorizeAsync(token, EventKey(Guid.NewGuid()), ResourceAction.Read)).Reason);
        Assert.False((await Service.AuthorizeAsync(token,
            AuthorizationResourceKeys.GlobalRaceControl, ResourceAction.Operate)).IsAllowed);
        var before = await UserJsonAsync(account.UserId);
        await Service.AuthorizeAsync(token, EventKey(first), ResourceAction.Read);
        Assert.Equal(before, await UserJsonAsync(account.UserId));
        Assert.Contains("System Administrator", before);
    }

    [Fact]
    public async Task Role_revocation_expiry_and_regrant_take_effect_on_next_decision()
    {
        var (account, token) = await CreateIdentityAsync();
        var eventId = await CreateEventAsync();
        var roleId = await GrantRoleAsync(account.Id, eventId, EventRole.EventOrganizer);
        var service = Service;
        Assert.True((await service.AuthorizeAsync(token, EventKey(eventId), ResourceAction.Read)).IsAllowed);
        await ExecuteAsync(
            """UPDATE "EventRoleAssignments" SET "RevokedAt" = CURRENT_TIMESTAMP WHERE "Id" = @roleId""", new { roleId });
        Assert.False((await service.AuthorizeAsync(token, EventKey(eventId), ResourceAction.Read)).IsAllowed);
        roleId = await GrantRoleAsync(account.Id, eventId, EventRole.EventOrganizer);
        Assert.True((await service.AuthorizeAsync(token, EventKey(eventId), ResourceAction.Read)).IsAllowed);
        await ExecuteAsync(
            """UPDATE "EventRoleAssignments" SET "ExpiresAt" = CURRENT_TIMESTAMP - interval '1 second' WHERE "Id" = @roleId""",
            new { roleId });
        Assert.False((await service.AuthorizeAsync(token, EventKey(eventId), ResourceAction.Read)).IsAllowed);
    }

    [Theory]
    [InlineData("inactive-user")]
    [InlineData("suspended-account")]
    [InlineData("revoked-session")]
    public async Task Inactive_identity_or_revoked_session_cannot_use_existing_roles(string state)
    {
        var (account, token) = await CreateIdentityAsync();
        var eventId = await CreateEventAsync();
        await GrantRoleAsync(account.Id, eventId, EventRole.EventOrganizer);
        Assert.True((await Service.AuthorizeAsync(token, EventKey(eventId), ResourceAction.Read)).IsAllowed);
        if (state == "revoked-session")
            await Sessions.RevokeAsync(token);
        else if (state == "inactive-user")
            await ExecuteAsync("""UPDATE "Users" SET "IsActive" = false WHERE "Id" = @UserId""", new { account.UserId });
        else
            await ExecuteAsync("""UPDATE "Accounts" SET "Status" = 'Suspended' WHERE "Id" = @Id""", new { account.Id });
        Assert.Equal(AuthorizationOutcome.Unauthenticated,
            (await Service.AuthorizeAsync(token, EventKey(eventId), ResourceAction.Read)).Outcome);
        Assert.Equal(state == "revoked-session", (await Store.GetAsync(account.Id, default))!.IsActive);
    }

    [Fact]
    public async Task Global_admin_is_explicit_revocable_and_does_not_imply_sensitive_permission()
    {
        var (account, token) = await CreateIdentityAsync();
        var eventId = await CreateEventAsync();
        await ExecuteAsync(
            """
            INSERT INTO "GlobalRoleAssignments" ("Id", "AccountId", "Role", "GrantedByAccountId", "GrantedAt")
            VALUES (@id, @accountId, 1, @accountId, CURRENT_TIMESTAMP - interval '1 minute')
            """, new { id = Guid.NewGuid(), accountId = account.Id });
        var service = Service;
        Assert.True((await service.AuthorizeAsync(token,
            AuthorizationResourceKeys.GlobalRaceControl, ResourceAction.Operate)).IsAllowed);
        Assert.True((await service.AuthorizeAsync(token, EventKey(eventId), ResourceAction.Read)).IsAllowed);

        var key = await CreateResourceAsync(ResourceKind.MedicalInformation, eventId);
        var scopedService = ScopedService();
        Assert.Equal(AuthorizationReason.MissingSensitiveGrant,
            (await scopedService.AuthorizeAsync(token, key, ResourceAction.Read)).Reason);
        var grantId = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO "SensitivePermissionGrants"
                ("Id", "AccountId", "EventId", "Permission", "Purpose", "GrantedByAccountId", "GrantedAt", "ExpiresAt")
            VALUES (@grantId, @accountId, @eventId, @permission, 'Synthetic emergency support',
                @accountId, CURRENT_TIMESTAMP - interval '1 minute', CURRENT_TIMESTAMP + interval '30 minutes')
            """, new { grantId, accountId = account.Id, eventId, permission = (int)Permission.ReadMedicalInformation });
        Assert.True((await scopedService.AuthorizeAsync(token, key, ResourceAction.Read)).IsAllowed);
        Assert.False((await scopedService.AuthorizeAsync(token, key, ResourceAction.Manage)).IsAllowed);
        var otherEventKey = await CreateResourceAsync(ResourceKind.MedicalInformation, await CreateEventAsync());
        Assert.False((await scopedService.AuthorizeAsync(token, otherEventKey, ResourceAction.Read)).IsAllowed);
        await ExecuteAsync(
            """UPDATE "SensitivePermissionGrants" SET "RevokedAt" = CURRENT_TIMESTAMP WHERE "Id" = @grantId""", new { grantId });
        Assert.False((await scopedService.AuthorizeAsync(token, key, ResourceAction.Read)).IsAllowed);
        await ExecuteAsync(
            """UPDATE "GlobalRoleAssignments" SET "RevokedAt" = CURRENT_TIMESTAMP WHERE "AccountId" = @accountId""",
            new { accountId = account.Id });
        Assert.False((await service.AuthorizeAsync(token,
            AuthorizationResourceKeys.GlobalRaceControl, ResourceAction.Operate)).IsAllowed);
    }

    [Fact]
    public async Task Escalation_attempts_fail_and_limited_assignment_scope_is_not_client_controlled()
    {
        var (account, token) = await CreateIdentityAsync();
        var eventId = await CreateEventAsync();
        await GrantRoleAsync(account.Id, eventId, EventRole.EventOrganizer);
        var roles = new ResourceKey(ResourceKind.EventRoles, eventId);
        Assert.False((await Service.AuthorizeAsync(token, roles, ResourceAction.GrantRole,
            account.Id, EventRole.RaceOfficer)).IsAllowed);
        Assert.False((await Service.AuthorizeAsync(token, roles, ResourceAction.GrantRole,
            Guid.NewGuid(), EventRole.EventOrganizer)).IsAllowed);
        Assert.False((await Service.AuthorizeAsync(token, roles, ResourceAction.GrantRole,
            Guid.NewGuid(), EventRole.RaceOfficer)).IsAllowed);
        var (target, _) = await CreateIdentityAsync();
        Assert.True((await Service.AuthorizeAsync(token, roles, ResourceAction.GrantRole,
            target.Id, EventRole.RaceOfficer)).IsAllowed);
        await ExecuteAsync("""UPDATE "Accounts" SET "Status" = 'Suspended' WHERE "Id" = @Id""", new { target.Id });
        Assert.False((await Service.AuthorizeAsync(token, roles, ResourceAction.GrantRole,
            target.Id, EventRole.RaceOfficer)).IsAllowed);
        Assert.False((await Service.AuthorizeAsync(token,
            AuthorizationResourceKeys.System, ResourceAction.Manage)).IsAllowed);
        await ExecuteAsync(
            """UPDATE "EventRoleAssignments" SET "Role" = @role WHERE "AccountId" = @accountId""",
            new { role = (int)EventRole.TechnicalStaff, accountId = account.Id });
        var key = await CreateResourceAsync(ResourceKind.Boats, eventId);
        var service = ScopedService();
        Assert.False((await service.AuthorizeAsync(token, key, ResourceAction.Resolve)).IsAllowed);
        await ExecuteAsync(
            """UPDATE "SyntheticAuthorizationResources" SET "AssignedAccountId" = @accountId WHERE "Id" = @Id""",
            new { accountId = account.Id, key.Id });
        Assert.True((await service.AuthorizeAsync(token, key, ResourceAction.Resolve)).IsAllowed);
        var forgedKind = key with { Kind = ResourceKind.MedicalInformation };
        Assert.Equal(AuthorizationReason.MissingResourceScope,
            (await service.AuthorizeAsync(token, forgedKind, ResourceAction.Read)).Reason);
    }

    [Fact]
    public async Task Schema_rejects_unknown_roles_scope_orphans_and_non_sensitive_grants()
    {
        var (account, _) = await CreateIdentityAsync();
        var eventId = await CreateEventAsync();
        var roleId = await GrantRoleAsync(account.Id, eventId, EventRole.RaceOfficer);
        foreach (var sql in new[]
        {
            """UPDATE "EventRoleAssignments" SET "Role" = 999 WHERE "Id" = @roleId""",
            """UPDATE "EventRoleAssignments" SET "EventId" = @unknown WHERE "Id" = @roleId""",
            """UPDATE "EventRoleAssignments" SET "AccountId" = @unknown WHERE "Id" = @roleId""",
            """UPDATE "EventRoleAssignments" SET "GrantedByAccountId" = @unknown WHERE "Id" = @roleId""",
            """UPDATE "EventRoleAssignments" SET "ExpiresAt" = "GrantedAt" WHERE "Id" = @roleId"""
        })
            await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteAsync(sql, new { roleId, unknown = Guid.NewGuid() }));
        foreach (var permission in new[] { Permission.ReadEvent, (Permission)999 })
            await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
                """
                INSERT INTO "SensitivePermissionGrants"
                    ("Id", "AccountId", "EventId", "Permission", "Purpose", "GrantedByAccountId", "GrantedAt", "ExpiresAt")
                VALUES (@id, @accountId, @eventId, @permission, 'Synthetic test', @accountId,
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + interval '1 hour')
                """, new { id = Guid.NewGuid(), accountId = account.Id, eventId, permission = (int)permission }));
        Assert.Null(await Store.GetAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Scheduled_grants_can_be_revoked_before_activation()
    {
        var (account, _) = await CreateIdentityAsync();
        var eventId = await CreateEventAsync();
        var grantTime = DateTimeOffset.UtcNow.AddHours(1);
        var revokedAt = DateTimeOffset.UtcNow;
        var globalRoleId = Guid.NewGuid();
        var eventRoleId = Guid.NewGuid();
        var sensitiveGrantId = Guid.NewGuid();

        await ExecuteAsync(
            """
            INSERT INTO "GlobalRoleAssignments" ("Id", "AccountId", "Role", "GrantedByAccountId", "GrantedAt")
            VALUES (@id, @accountId, 1, @accountId, @grantTime)
            """, new { id = globalRoleId, accountId = account.Id, grantTime });
        await ExecuteAsync(
            """
            INSERT INTO "EventRoleAssignments" ("Id", "AccountId", "EventId", "Role", "GrantedByAccountId", "GrantedAt")
            VALUES (@id, @accountId, @eventId, 1, @accountId, @grantTime)
            """, new { id = eventRoleId, accountId = account.Id, eventId, grantTime });
        await ExecuteAsync(
            """
            INSERT INTO "SensitivePermissionGrants"
                ("Id", "AccountId", "EventId", "Permission", "Purpose", "GrantedByAccountId", "GrantedAt", "ExpiresAt")
            VALUES (@id, @accountId, @eventId, 103, 'Synthetic scheduled grant', @accountId, @grantTime, @expiresAt)
            """, new { id = sensitiveGrantId, accountId = account.Id, eventId, grantTime, expiresAt = grantTime.AddHours(1) });

        await ExecuteAsync(
            """UPDATE "GlobalRoleAssignments" SET "RevokedAt" = @revokedAt WHERE "Id" = @id""",
            new { id = globalRoleId, revokedAt });
        await ExecuteAsync(
            """UPDATE "EventRoleAssignments" SET "RevokedAt" = @revokedAt WHERE "Id" = @id""",
            new { id = eventRoleId, revokedAt });
        await ExecuteAsync(
            """UPDATE "SensitivePermissionGrants" SET "RevokedAt" = @revokedAt WHERE "Id" = @id""",
            new { id = sensitiveGrantId, revokedAt });
    }

    [Fact]
    public async Task Default_resolver_denies_unimplemented_child_and_document_scope()
    {
        var resolver = new PostgresAuthorizationResourceResolver(database.DataSource);
        foreach (var kind in Enum.GetValues<ResourceKind>().Where(k =>
            k is not (ResourceKind.Event or ResourceKind.EventRoles)))
            Assert.Null(await resolver.ResolveAsync(new(kind, Guid.NewGuid()), default));
        Assert.Null(await resolver.ResolveAsync(new(ResourceKind.Event, Guid.Empty), default));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Service.AuthorizeAsync(new string('A', 64), EventKey(Guid.NewGuid()),
                ResourceAction.Read, cancellationToken: cancellation.Token));
    }

    private EventAuthorizationService ScopedService() => new(
        Sessions, Store, new SyntheticResourceResolver(database.DataSource), _clock);

    private async Task<(Account Account, string Token)> CreateIdentityAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"rbac-{userId:N}@example.invalid";
        await ExecuteAsync(
            """INSERT INTO "Users" ("Id", "Name", "Email", "Role") VALUES (@userId, 'Synthetic RBAC', @email, 'System Administrator')""",
            new { userId, email });
        var repository = new AccountRepository(database.DataSource);
        var hasher = new AccountPasswordHasher();
        var accounts = new AccountService(repository, hasher, _clock);
        var account = await accounts.CreatePendingAsync(userId);
        account = await accounts.ActivateAsync(account.Id, Password);
        var sessions = new AccountSessionRepository(database.DataSource);
        await sessions.BindLoginEmailAsync(account.Id, email, default);
        var issued = await new IndividualAuthenticationService(repository, sessions, hasher, _clock)
            .AuthenticateAsync(email, Password);
        return (account, Assert.IsType<IssuedAccountSession>(issued).Token);
    }

    private async Task<Guid> CreateEventAsync()
    {
        var id = Guid.NewGuid();
        await ExecuteAsync("""INSERT INTO "Events" ("Id") VALUES (@id)""", new { id });
        return id;
    }

    private async Task<Guid> GrantRoleAsync(Guid accountId, Guid eventId, EventRole role)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO "EventRoleAssignments" ("Id", "AccountId", "EventId", "Role", "GrantedByAccountId", "GrantedAt")
            VALUES (@id, @accountId, @eventId, @role, @accountId, CURRENT_TIMESTAMP - interval '1 minute')
            """, new { id, accountId, eventId, role = (int)role });
        return id;
    }

    private async Task<ResourceKey> CreateResourceAsync(ResourceKind kind, Guid eventId)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(
            """INSERT INTO "SyntheticAuthorizationResources" ("Id", "Kind", "EventId") VALUES (@id, @kind, @eventId)""",
            new { id, kind = (int)kind, eventId });
        return new(kind, id);
    }

    private async Task ExecuteAsync(string sql, object parameters)
    {
        await using var connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(sql, parameters);
    }

    private async Task<string> UserJsonAsync(Guid id)
    {
        await using var connection = await database.DataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<string>(
            """SELECT row_to_json(u)::text FROM "Users" u WHERE "Id" = @id""", new { id });
    }

    private static ResourceKey EventKey(Guid eventId) => new(ResourceKind.Event, eventId);

    private sealed class SyntheticResourceResolver(NpgsqlDataSource dataSource) : IAuthorizationResourceResolver
    {
        public async Task<AuthorizationResource?> ResolveAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            var row = await connection.QuerySingleOrDefaultAsync<ScopeRow>(new CommandDefinition(
                """SELECT "EventId", "AssignedAccountId" FROM "SyntheticAuthorizationResources" WHERE "Id" = @Id AND "Kind" = @kind""",
                new { key.Id, kind = (int)key.Kind }, cancellationToken: cancellationToken));
            return row is null ? null : new(key, ResourceScope.Event, row.EventId, row.AssignedAccountId);
        }

        private sealed class ScopeRow
        {
            public Guid EventId { get; set; }
            public Guid? AssignedAccountId { get; set; }
        }
    }
}
