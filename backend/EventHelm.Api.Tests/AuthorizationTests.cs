using EventHelm.Api.Authorization;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Xunit;

namespace EventHelm.Api.Tests;

public sealed class AuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _event = Guid.NewGuid();
    private readonly MemoryStore _store = new();
    private readonly MemoryResources _resources = new();
    private readonly SessionStub _sessions = new();
    private const string Token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Theory]
    [InlineData(null)]
    [InlineData("shared-password-cookie")]
    [InlineData("role=SystemAdministrator")]
    [InlineData("invalid")]
    public async Task Only_individual_session_identity_is_accepted(string? token)
    {
        var service = Setup(EventRole.EventOrganizer);
        Assert.Equal(AuthorizationOutcome.Unauthenticated,
            (await service.AuthorizeAsync(token, EventKey, ResourceAction.Read)).Outcome);
    }

    [Fact]
    public async Task Revoked_or_expired_session_is_unauthenticated_not_forbidden()
    {
        var service = Setup(EventRole.EventOrganizer);
        _sessions.Session = null;
        var result = await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read);
        Assert.Equal(AuthorizationOutcome.Unauthenticated, result.Outcome);
        Assert.Equal(AuthorizationReason.InvalidSession, result.Reason);
    }

    [Fact]
    public async Task Cross_event_and_unrelated_account_roles_never_grant_access()
    {
        var service = Setup(EventRole.EventOrganizer);
        Assert.True((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).IsAllowed);
        var otherEvent = Guid.NewGuid();
        var otherKey = new ResourceKey(ResourceKind.Event, otherEvent);
        _resources.Items[otherKey] = new(otherKey, ResourceScope.Event, otherEvent);
        Assert.Equal(AuthorizationReason.MissingMembership,
            (await service.AuthorizeAsync(Token, otherKey, ResourceAction.Read)).Reason);
        _store.Snapshot = _store.Snapshot! with
        {
            EventRoles = [Role(EventRole.EventOrganizer) with { AccountId = Guid.NewGuid() }]
        };
        Assert.False((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).IsAllowed);
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("unknown")]
    public async Task Invalid_role_lifetimes_and_unknown_roles_deny(string state)
    {
        var service = Setup(EventRole.EventOrganizer);
        var role = Role(EventRole.EventOrganizer);
        role = state switch
        {
            "revoked" => role with { RevokedAt = Now },
            "expired" => role with { ExpiresAt = Now },
            "future" => role with { GrantedAt = Now.AddMinutes(1) },
            _ => role with { Role = (EventRole)999 }
        };
        _store.Snapshot = _store.Snapshot! with { EventRoles = [role] };
        Assert.False((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).IsAllowed);
    }

    [Fact]
    public async Task Missing_or_inactive_account_and_mismatched_identity_deny()
    {
        var service = Setup(EventRole.EventOrganizer);
        var original = _store.Snapshot!;
        foreach (var snapshot in new AuthorizationSnapshot?[]
        {
            null, original with { IsActive = false }, original with { AccountId = Guid.NewGuid() }
        })
        {
            _store.Snapshot = snapshot;
            Assert.Equal(AuthorizationReason.InactiveAccount,
                (await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).Reason);
        }
    }

    [Fact]
    public async Task Unknown_actions_missing_scope_and_forged_resource_context_deny_even_admin()
    {
        var service = SetupAdministrator();
        Assert.False((await service.AuthorizeAsync(Token, null, ResourceAction.Read)).IsAllowed);
        Assert.False((await service.AuthorizeAsync(Token, EventKey, (ResourceAction)999)).IsAllowed);
        Assert.False((await service.AuthorizeAsync(Token, new((ResourceKind)999, _event), ResourceAction.Read)).IsAllowed);
        Assert.False((await service.AuthorizeAsync(Token, new(ResourceKind.Event, Guid.Empty), ResourceAction.Read)).IsAllowed);
        foreach (var resource in new AuthorizationResource?[]
        {
            null, new(EventKey, ResourceScope.Event), new(EventKey, ResourceScope.Event, Guid.Empty),
            new(EventKey, ResourceScope.Event, Guid.NewGuid()), new(EventKey, (ResourceScope)999, _event),
            new(new(ResourceKind.Event, Guid.NewGuid()), ResourceScope.Event, _event),
            new(EventKey, ResourceScope.Global)
        })
        {
            _resources.Items[EventKey] = resource;
            Assert.False((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).IsAllowed);
        }
    }

    [Theory]
    [InlineData(EventRole.EventOrganizer, false)]
    [InlineData(EventRole.RaceOfficer, true)]
    [InlineData(EventRole.TechnicalStaff, false)]
    [InlineData(EventRole.OnWaterPersonnel, false)]
    public async Task Event_race_control_requires_race_officer_not_organizer(EventRole role, bool allowed)
    {
        var service = Setup(role);
        var key = AddResource(ResourceKind.RaceControl);
        Assert.Equal(allowed, (await service.AuthorizeAsync(Token, key, ResourceAction.Operate)).IsAllowed);
        var global = AuthorizationResourceKeys.GlobalRaceControl;
        _resources.Items[global] = new(global, ResourceScope.Global);
        Assert.False((await service.AuthorizeAsync(Token, global, ResourceAction.Read)).IsAllowed);
        Assert.False((await service.AuthorizeAsync(Token, global, ResourceAction.Operate)).IsAllowed);
    }

    [Fact]
    public async Task Global_administrator_is_separate_and_can_access_global_race_control()
    {
        var service = SetupAdministrator();
        foreach (var key in new[] { AuthorizationResourceKeys.GlobalRaceControl, AuthorizationResourceKeys.GlobalInventory,
            AuthorizationResourceKeys.System, AuthorizationResourceKeys.EventCreation })
        {
            _resources.Items[key] = new(key, ResourceScope.Global);
            var action = key.Kind switch
            {
                ResourceKind.RaceControl => ResourceAction.Operate,
                ResourceKind.Event => ResourceAction.Create,
                _ => ResourceAction.Manage
            };
            Assert.True((await service.AuthorizeAsync(Token, key, action)).IsAllowed);
        }
        Assert.True((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).IsAllowed);
        _store.Snapshot = _store.Snapshot! with
        {
            GlobalRoles = [new(Guid.NewGuid(), _account, (GlobalRole)999, Now.AddMinutes(-1))]
        };
        Assert.False((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).IsAllowed);
    }

    [Theory]
    [InlineData(EventRole.TechnicalStaff)]
    [InlineData(EventRole.OnWaterPersonnel)]
    public async Task Limited_roles_require_server_verified_assignment(EventRole role)
    {
        var service = Setup(role);
        foreach (var (kind, action) in new[]
        {
            (ResourceKind.Boats, ResourceAction.Read), (ResourceKind.Boats, ResourceAction.Report),
            (ResourceKind.Tasks, ResourceAction.UpdateOwn), (ResourceKind.Assignments, ResourceAction.Read),
            (ResourceKind.Shifts, ResourceAction.Read), (ResourceKind.Communications, ResourceAction.Operate)
        })
        {
            var key = AddResource(kind);
            Assert.False((await service.AuthorizeAsync(Token, key, action)).IsAllowed);
            _resources.Items[key] = _resources.Items[key]! with { AssignedAccountId = Guid.NewGuid() };
            Assert.False((await service.AuthorizeAsync(Token, key, action)).IsAllowed);
            _resources.Items[key] = _resources.Items[key]! with { AssignedAccountId = _account };
            Assert.True((await service.AuthorizeAsync(Token, key, action)).IsAllowed);
        }
        var boat = AddResource(ResourceKind.Boats, assigned: true);
        Assert.Equal(role == EventRole.TechnicalStaff,
            (await service.AuthorizeAsync(Token, boat, ResourceAction.Resolve)).IsAllowed);
        Assert.False((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Manage)).IsAllowed);
    }

    [Fact]
    public async Task Organizer_cannot_escalate_self_or_appoint_organizers_and_other_roles_cannot_grant()
    {
        var service = Setup(EventRole.EventOrganizer);
        var key = AddResource(ResourceKind.EventRoles);
        foreach (var (targetId, role) in new (Guid?, EventRole?)[]
        {
            (null, EventRole.RaceOfficer), (Guid.Empty, EventRole.RaceOfficer),
            (_account, EventRole.RaceOfficer), (Guid.NewGuid(), EventRole.EventOrganizer),
            (Guid.NewGuid(), null), (Guid.NewGuid(), (EventRole)999)
        })
            Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.GrantRole, targetId, role)).IsAllowed);
        var target = Guid.NewGuid();
        Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.GrantRole,
            target, EventRole.RaceOfficer)).IsAllowed);
        _store.Targets[target] = new(target, false, [], [], []);
        Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.GrantRole,
            target, EventRole.RaceOfficer)).IsAllowed);
        _store.Targets[target] = _store.Targets[target] with { IsActive = true };
        Assert.True((await service.AuthorizeAsync(Token, key, ResourceAction.GrantRole,
            target, EventRole.RaceOfficer)).IsAllowed);
        _store.Snapshot = _store.Snapshot! with { EventRoles = [Role(EventRole.RaceOfficer)] };
        Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.GrantRole,
            target, EventRole.TechnicalStaff)).IsAllowed);
    }

    [Theory]
    [InlineData(ResourceKind.PersonalInformation, Permission.ReadPersonalInformation)]
    [InlineData(ResourceKind.MedicalInformation, Permission.ReadMedicalInformation)]
    [InlineData(ResourceKind.Documents, Permission.ReadPrivateDocuments)]
    public async Task Sensitive_information_requires_explicit_purpose_grant_even_for_admin(
        ResourceKind kind, Permission permission)
    {
        var service = SetupAdministrator();
        var key = AddResource(kind);
        if (kind == ResourceKind.Documents)
            _resources.Items[key] = _resources.Items[key]! with
            {
                IsPrivateDocument = true, DocumentStorage = DocumentStorage.Private
            };
        Assert.Equal(AuthorizationReason.MissingSensitiveGrant,
            (await service.AuthorizeAsync(Token, key, ResourceAction.Read)).Reason);
        var grant = new SensitivePermissionGrant(Guid.NewGuid(), _account, _event, permission,
            "Synthetic operational purpose", Now.AddMinutes(-1), Now.AddMinutes(30));
        _store.Snapshot = _store.Snapshot! with { SensitiveGrants = [grant] };
        Assert.True((await service.AuthorizeAsync(Token, key, ResourceAction.Read)).IsAllowed);
        Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.Manage)).IsAllowed);
        foreach (var invalid in new[]
        {
            grant with { AccountId = Guid.NewGuid() }, grant with { EventId = Guid.NewGuid() },
            grant with { Purpose = " " }, grant with { ExpiresAt = Now }, grant with { RevokedAt = Now },
            grant with { GrantedAt = Now.AddMinutes(1) }, grant with { Permission = (Permission)999 }
        })
        {
            _store.Snapshot = _store.Snapshot! with { SensitiveGrants = [invalid] };
            Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.Read)).IsAllowed);
        }
        _store.Snapshot = _store.Snapshot! with { GlobalRoles = [], EventRoles = [], SensitiveGrants = [grant] };
        Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.Read)).IsAllowed);
    }

    [Fact]
    public async Task Public_static_and_unclassified_document_storage_deny_even_admin()
    {
        var service = SetupAdministrator();
        var key = AddResource(ResourceKind.Documents);
        foreach (var storage in new[] { DocumentStorage.NotApplicable, DocumentStorage.PublicStatic })
        {
            _resources.Items[key] = _resources.Items[key]! with { DocumentStorage = storage };
            Assert.Equal(AuthorizationReason.UnsafeDocumentStorage,
                (await service.AuthorizeAsync(Token, key, ResourceAction.Read)).Reason);
        }
    }

    [Fact]
    public async Task Combined_roles_work_and_role_changes_refresh_without_cached_decisions()
    {
        var service = Setup(EventRole.EventOrganizer);
        var key = AddResource(ResourceKind.RaceControl);
        Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.Operate)).IsAllowed);
        _store.Snapshot = _store.Snapshot! with
        {
            EventRoles = [Role(EventRole.EventOrganizer), Role(EventRole.RaceOfficer)]
        };
        Assert.True((await service.AuthorizeAsync(Token, key, ResourceAction.Operate)).IsAllowed);
        _store.Snapshot = _store.Snapshot! with { EventRoles = [Role(EventRole.EventOrganizer)] };
        Assert.False((await service.AuthorizeAsync(Token, key, ResourceAction.Operate)).IsAllowed);
    }

    [Fact]
    public async Task Storage_failures_propagate_instead_of_allowing_stale_access()
    {
        var service = Setup(EventRole.EventOrganizer);
        _store.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AuthorizeAsync(Token, EventKey, ResourceAction.Read));
    }

    [Fact]
    public void Matrix_never_grants_sensitive_or_global_permissions_through_event_roles()
    {
        foreach (var role in Enum.GetValues<EventRole>())
        foreach (var permission in Enum.GetValues<Permission>().Where(p =>
            RolePermissions.IsSensitive(p) || p is Permission.ManageSystem or Permission.CreateEvent or Permission.ManageGlobalInventory))
            Assert.False(RolePermissions.Allows(role, permission));
        Assert.False(RolePermissions.Allows((EventRole)999, Permission.ReadEvent));
        Assert.Null(RolePermissions.For(ResourceKind.Documents, ResourceAction.GrantRole));
    }

    [Theory]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Event, ResourceAction.Manage, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Event, ResourceAction.Manage, false)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Event, ResourceAction.Plan, true)]
    [InlineData(EventRole.TechnicalStaff, ResourceKind.Event, ResourceAction.Plan, false)]
    [InlineData(EventRole.OnWaterPersonnel, ResourceKind.Event, ResourceAction.Read, false)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Officials, ResourceAction.Manage, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Officials, ResourceAction.Manage, false)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Officials, ResourceAction.Read, true)]
    [InlineData(EventRole.TechnicalStaff, ResourceKind.Officials, ResourceAction.Read, false)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Boats, ResourceAction.Allocate, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Boats, ResourceAction.Allocate, true)]
    [InlineData(EventRole.TechnicalStaff, ResourceKind.Boats, ResourceAction.Allocate, true)]
    [InlineData(EventRole.OnWaterPersonnel, ResourceKind.Boats, ResourceAction.Allocate, false)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Boats, ResourceAction.Resolve, false)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Boats, ResourceAction.Resolve, false)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Assignments, ResourceAction.Manage, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Assignments, ResourceAction.Manage, true)]
    [InlineData(EventRole.TechnicalStaff, ResourceKind.Assignments, ResourceAction.Manage, false)]
    [InlineData(EventRole.OnWaterPersonnel, ResourceKind.Assignments, ResourceAction.Manage, false)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Shifts, ResourceAction.Manage, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Shifts, ResourceAction.Manage, true)]
    [InlineData(EventRole.TechnicalStaff, ResourceKind.Shifts, ResourceAction.Manage, false)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Tasks, ResourceAction.Manage, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Tasks, ResourceAction.Manage, true)]
    [InlineData(EventRole.OnWaterPersonnel, ResourceKind.Tasks, ResourceAction.Manage, false)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Communications, ResourceAction.Operate, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Communications, ResourceAction.Operate, true)]
    [InlineData(EventRole.TechnicalStaff, ResourceKind.Communications, ResourceAction.Operate, true)]
    [InlineData(EventRole.OnWaterPersonnel, ResourceKind.Communications, ResourceAction.Operate, true)]
    [InlineData(EventRole.EventOrganizer, ResourceKind.Documents, ResourceAction.Manage, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Documents, ResourceAction.Read, true)]
    [InlineData(EventRole.RaceOfficer, ResourceKind.Documents, ResourceAction.Manage, false)]
    [InlineData(EventRole.TechnicalStaff, ResourceKind.Documents, ResourceAction.Read, false)]
    [InlineData(EventRole.OnWaterPersonnel, ResourceKind.Documents, ResourceAction.Read, false)]
    public async Task Operational_matrix_is_explicit(
        EventRole role, ResourceKind kind, ResourceAction action, bool allowed)
    {
        var service = Setup(role);
        var key = kind == ResourceKind.Event ? EventKey : AddResource(kind, assigned: true);
        if (kind == ResourceKind.Documents)
            _resources.Items[key] = _resources.Items[key]! with { DocumentStorage = DocumentStorage.Private };
        Assert.Equal(allowed, (await service.AuthorizeAsync(Token, key, action)).IsAllowed);
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("other-account")]
    public async Task Global_role_eligibility_is_checked_each_time(string state)
    {
        var service = SetupAdministrator();
        var role = Assert.Single(_store.Snapshot!.GlobalRoles);
        role = state switch
        {
            "revoked" => role with { RevokedAt = Now },
            "expired" => role with { ExpiresAt = Now },
            "future" => role with { GrantedAt = Now.AddMinutes(1) },
            _ => role with { AccountId = Guid.NewGuid() }
        };
        _store.Snapshot = _store.Snapshot with { GlobalRoles = [role] };
        Assert.False((await service.AuthorizeAsync(Token, EventKey, ResourceAction.Read)).IsAllowed);
    }

    private ResourceKey EventKey => new(ResourceKind.Event, _event);

    private EventAuthorizationService Setup(EventRole role)
    {
        _sessions.Session = new(_account, Guid.NewGuid(), Now.AddHours(1));
        _store.Snapshot = new(_account, true, [], [Role(role)], []);
        _resources.Items[EventKey] = new(EventKey, ResourceScope.Event, _event);
        return new(new(_sessions, new FixedClock()), _store, _resources, new FixedClock());
    }

    private EventAuthorizationService SetupAdministrator()
    {
        var service = Setup(EventRole.EventOrganizer);
        _store.Snapshot = _store.Snapshot! with
        {
            EventRoles = [], GlobalRoles = [new(Guid.NewGuid(), _account,
                GlobalRole.SystemAdministrator, Now.AddMinutes(-1))]
        };
        return service;
    }

    private EventRoleAssignment Role(EventRole role) =>
        new(Guid.NewGuid(), _account, _event, role, Now.AddMinutes(-1));

    private ResourceKey AddResource(ResourceKind kind, bool assigned = false)
    {
        var key = new ResourceKey(kind, Guid.NewGuid());
        _resources.Items[key] = new(key, ResourceScope.Event, _event, assigned ? _account : null);
        return key;
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryStore : IAuthorizationStore
    {
        public AuthorizationSnapshot? Snapshot { get; set; }
        public Dictionary<Guid, AuthorizationSnapshot> Targets { get; } = [];
        public bool Fail { get; set; }
        public Task<AuthorizationSnapshot?> GetAsync(Guid accountId, CancellationToken cancellationToken) =>
            Fail ? throw new InvalidOperationException("Synthetic storage failure.") :
                Task.FromResult(Targets.TryGetValue(accountId, out var target) ? target : Snapshot);
    }

    private sealed class MemoryResources : IAuthorizationResourceResolver
    {
        public Dictionary<ResourceKey, AuthorizationResource?> Items { get; } = [];
        public Task<AuthorizationResource?> ResolveAsync(ResourceKey key, CancellationToken cancellationToken) =>
            Task.FromResult(Items.GetValueOrDefault(key));
    }

    private sealed class SessionStub : IAccountSessionRepository
    {
        public ValidAccountSession? Session { get; set; }
        public Task<ValidAccountSession?> ValidateAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(Session);
        public Task BindLoginEmailAsync(Guid accountId, string normalizedEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryReserveAttemptAsync(string identifierHash, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RecordFailureAsync(string identifierHash, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AccountLogin?> FindLoginAsync(string normalizedEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryCreateSessionAsync(AccountLogin login, string identifierHash, string tokenHash, DateTimeOffset now,
            DateTimeOffset expiresAt, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RevokeAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RevokeAllAsync(Guid accountId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
