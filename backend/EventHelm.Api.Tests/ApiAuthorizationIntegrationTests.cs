using System.Security.Cryptography;
using System.Text;
using EventHelm.Api.Authorization;
using EventHelm.Api.Authorization.Integration;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Xunit;

namespace EventHelm.Api.Tests;

public sealed class ApiAuthorizationIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
    private const string Token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _event = Guid.NewGuid();
    private readonly SessionStub _sessions = new();
    private readonly MemoryStore _store = new();
    private readonly MemoryResources _resources = new();

    [Theory]
    [InlineData(null)]
    [InlineData("EventHelm.Access")]
    [InlineData("Users.Role=SystemAdministrator")]
    [InlineData("frontend-role=EventOrganizer")]
    [InlineData("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB")]
    public async Task Missing_or_untrusted_credentials_return_401_before_scope_lookup(string? token)
    {
        var service = Setup();
        var decision = await service.AuthorizeAllAsync(token, null);
        Assert.Equal(401, AuthorizationHttpStatus.DenialStatusCode(decision));
        Assert.Equal(AuthorizationReason.InvalidSession, decision.Reason);
        Assert.Equal(0, _resources.Reads);
        Assert.Equal(0, _store.Reads);
    }

    [Fact]
    public async Task Valid_individual_session_can_read_verified_event()
    {
        var service = Setup();
        var decision = await service.AuthorizeAllAsync(Token, [EventRead]);
        Assert.True(decision.IsAllowed);
        Assert.Null(AuthorizationHttpStatus.DenialStatusCode(decision));
        Assert.Equal(1, _resources.Reads);
    }

    [Fact]
    public async Task Insufficient_permission_is_403_not_a_login_failure()
    {
        var service = Setup(EventRole.RaceOfficer);
        var decision = await service.AuthorizeAllAsync(Token, [EventRead with { Action = ResourceAction.Manage }]);
        Assert.Equal(403, AuthorizationHttpStatus.DenialStatusCode(decision));
        Assert.Equal(AuthorizationReason.MissingPermission, decision.Reason);
    }

    [Fact]
    public async Task Cross_event_membership_does_not_authorize_another_event()
    {
        var service = Setup();
        var otherEvent = Guid.NewGuid();
        var key = new ResourceKey(ResourceKind.Event, otherEvent);
        _resources.Items[key] = new(key, ResourceScope.Event, otherEvent);
        var decision = await service.AuthorizeAllAsync(Token,
            [new(key, ResourceAction.Read, ResourceScope.Event, otherEvent)]);
        Assert.Equal(AuthorizationReason.MissingMembership, decision.Reason);
        Assert.Equal(403, AuthorizationHttpStatus.DenialStatusCode(decision));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Spoofed_route_or_body_event_cannot_relabel_a_child_even_for_admin(bool administrator)
    {
        var service = Setup(administrator: administrator);
        var child = Add(ResourceKind.Tasks);
        _resources.Items[child.Key] = _resources.Items[child.Key]! with { EventId = Guid.NewGuid() };
        var decision = await service.AuthorizeAllAsync(Token, [child]);
        Assert.Equal(AuthorizationReason.MissingResourceScope, decision.Reason);
        Assert.False(decision.IsAllowed);
        Assert.Equal(1, _resources.Reads);
    }

    [Fact]
    public async Task Missing_empty_or_ambiguous_operation_metadata_denies_by_default()
    {
        var service = Setup(administrator: true);
        foreach (var requirements in new IReadOnlyList<ApiAuthorizationRequirement?>?[]
        {
            null, [], [null],
            [EventRead with { ExpectedEventId = null }],
            [EventRead with { ExpectedEventId = Guid.Empty }],
            [EventRead with { Key = new(ResourceKind.Event, Guid.Empty) }],
            [EventRead with { Key = null! }],
            [EventRead with { Action = (ResourceAction)999 }],
            [EventRead with { Scope = (ResourceScope)999 }],
            [EventRead with { Scope = ResourceScope.Global, ExpectedEventId = null }],
            [new(AuthorizationResourceKeys.GlobalRaceControl, ResourceAction.Read, ResourceScope.Event, _event)],
            [new(AuthorizationResourceKeys.System, ResourceAction.Manage, ResourceScope.Global, _event)]
        })
        {
            var decision = await service.AuthorizeAllAsync(Token, requirements);
            Assert.Equal(AuthorizationReason.InvalidRequest, decision.Reason);
            Assert.Equal(403, AuthorizationHttpStatus.DenialStatusCode(decision));
        }
        Assert.Equal(0, _resources.Reads);
    }

    [Fact]
    public async Task Unresolved_or_mismatched_canonical_scope_never_falls_back_to_parent_even_for_admin()
    {
        var service = Setup(administrator: true);
        var child = Add(ResourceKind.Assignments);
        foreach (var resource in new AuthorizationResource?[]
        {
            null,
            new(child.Key, ResourceScope.Event),
            new(child.Key, ResourceScope.Event, Guid.Empty),
            new(child.Key, ResourceScope.Global),
            new(child.Key, (ResourceScope)999, _event),
            new(new(ResourceKind.MedicalInformation, child.Key.Id), ResourceScope.Event, _event)
        })
        {
            _resources.Items[child.Key] = resource;
            var decision = await service.AuthorizeAllAsync(Token, [child]);
            Assert.Equal(AuthorizationReason.MissingResourceScope, decision.Reason);
            Assert.False(decision.IsAllowed);
        }
    }

    [Fact]
    public async Task Revocation_expiry_or_ineligibility_at_session_validation_returns_401()
    {
        var service = Setup();
        Assert.True((await service.AuthorizeAllAsync(Token, [EventRead])).IsAllowed);
        // The real repository rejects revoked/expired sessions, inactive accounts/users
        // and changed account/security versions with this same null result.
        _sessions.Session = null;
        var decision = await service.AuthorizeAllAsync(Token, [EventRead]);
        Assert.Equal(401, AuthorizationHttpStatus.DenialStatusCode(decision));
    }

    [Fact]
    public async Task Account_disabled_after_session_validation_and_revoked_grants_deny_on_next_check()
    {
        var service = Setup();
        Assert.True((await service.AuthorizeAllAsync(Token, [EventRead])).IsAllowed);
        var original = _store.Snapshot!;
        _store.Snapshot = original with { IsActive = false };
        var decision = await service.AuthorizeAllAsync(Token, [EventRead]);
        Assert.Equal(AuthorizationReason.InactiveAccount, decision.Reason);
        Assert.Equal(403, AuthorizationHttpStatus.DenialStatusCode(decision));
        _store.Snapshot = original with { EventRoles = [original.EventRoles[0] with { RevokedAt = Now }] };
        Assert.False((await service.AuthorizeAllAsync(Token, [EventRead])).IsAllowed);
        _store.Snapshot = original with { AccountId = Guid.NewGuid() };
        Assert.False((await service.AuthorizeAllAsync(Token, [EventRead])).IsAllowed);
    }

    [Theory]
    [InlineData(ResourceKind.PersonalInformation, Permission.ReadPersonalInformation)]
    [InlineData(ResourceKind.MedicalInformation, Permission.ReadMedicalInformation)]
    [InlineData(ResourceKind.Documents, Permission.ReadPrivateDocuments)]
    public async Task Sensitive_fields_need_separate_current_event_grants_including_for_administrators(
        ResourceKind kind, Permission permission)
    {
        var service = Setup(administrator: true);
        var sensitive = Add(kind);
        if (kind == ResourceKind.Documents)
            _resources.Items[sensitive.Key] = _resources.Items[sensitive.Key]! with
            {
                IsPrivateDocument = true, DocumentStorage = DocumentStorage.Private
            };
        Assert.Equal(AuthorizationReason.MissingSensitiveGrant,
            (await service.AuthorizeAllAsync(Token, [EventRead, sensitive])).Reason);
        var grant = new SensitivePermissionGrant(Guid.NewGuid(), _account, _event, permission,
            "Synthetic operational purpose", Now.AddMinutes(-1), Now.AddMinutes(10));
        _store.Snapshot = _store.Snapshot! with { SensitiveGrants = [grant with { EventId = Guid.NewGuid() }] };
        Assert.False((await service.AuthorizeAllAsync(Token, [sensitive])).IsAllowed);
        _store.Snapshot = _store.Snapshot with { SensitiveGrants = [grant] };
        Assert.True((await service.AuthorizeAllAsync(Token, [EventRead, sensitive])).IsAllowed);
        Assert.False((await service.AuthorizeAllAsync(Token, [sensitive with { Action = ResourceAction.Manage }])).IsAllowed);
        _store.Snapshot = _store.Snapshot with { SensitiveGrants = [grant with { RevokedAt = Now }] };
        Assert.False((await service.AuthorizeAllAsync(Token, [sensitive])).IsAllowed);
        _store.Snapshot = _store.Snapshot with { SensitiveGrants = [grant with { ExpiresAt = Now }] };
        Assert.False((await service.AuthorizeAllAsync(Token, [sensitive])).IsAllowed);
    }

    [Theory]
    [InlineData(DocumentStorage.NotApplicable)]
    [InlineData(DocumentStorage.PublicStatic)]
    public async Task Public_or_unknown_document_storage_denies_even_for_admin(DocumentStorage storage)
    {
        var service = Setup(administrator: true);
        var document = Add(ResourceKind.Documents);
        _resources.Items[document.Key] = _resources.Items[document.Key]! with { DocumentStorage = storage };
        Assert.Equal(AuthorizationReason.UnsafeDocumentStorage,
            (await service.AuthorizeAllAsync(Token, [document])).Reason);
    }

    [Fact]
    public async Task Every_requirement_must_pass_and_limited_roles_need_authoritative_assignment()
    {
        var service = Setup(EventRole.TechnicalStaff);
        var boat = Add(ResourceKind.Boats);
        var task = Add(ResourceKind.Tasks) with { Action = ResourceAction.UpdateOwn };
        _resources.Items[boat.Key] = _resources.Items[boat.Key]! with { AssignedAccountId = _account };
        Assert.False((await service.AuthorizeAllAsync(Token, [boat, task])).IsAllowed);
        _resources.Items[task.Key] = _resources.Items[task.Key]! with { AssignedAccountId = Guid.NewGuid() };
        Assert.False((await service.AuthorizeAllAsync(Token, [boat, task])).IsAllowed);
        _resources.Items[task.Key] = _resources.Items[task.Key]! with { AssignedAccountId = _account };
        Assert.True((await service.AuthorizeAllAsync(Token, [boat, task])).IsAllowed);
        Assert.False((await service.AuthorizeAllAsync(Token, [task with { Action = ResourceAction.Manage }])).IsAllowed);
    }

    [Theory]
    [InlineData(ResourceAction.Read)]
    [InlineData(ResourceAction.Operate)]
    public async Task Existing_global_race_control_is_only_for_current_global_administrators(ResourceAction action)
    {
        var service = Setup(EventRole.RaceOfficer);
        var key = AuthorizationResourceKeys.GlobalRaceControl;
        _resources.Items[key] = new(key, ResourceScope.Global);
        var requirement = new ApiAuthorizationRequirement(key, action, ResourceScope.Global);
        Assert.False((await service.AuthorizeAllAsync(Token, [requirement])).IsAllowed);
        MakeAdministrator();
        var decision = await service.AuthorizeAllAsync(Token, [requirement]);
        Assert.Equal(AuthorizationReason.AdministratorOverride, decision.Reason);
        _resources.Items[key] = new(key, ResourceScope.Event, _event);
        Assert.False((await service.AuthorizeAllAsync(Token, [requirement])).IsAllowed);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("store")]
    [InlineData("resolver")]
    public async Task Infrastructure_failures_propagate_instead_of_becoming_success(string source)
    {
        var service = Setup();
        _sessions.Fail = source == "session";
        _store.Fail = source == "store";
        _resources.Fail = source == "resolver";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AuthorizeAllAsync(Token, [EventRead]));
    }

    [Fact]
    public async Task Cancellation_propagates()
    {
        var service = Setup();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.AuthorizeAllAsync(Token, [EventRead], cancellation.Token));
    }

    [Fact]
    public void Unknown_http_outcome_denies()
    {
        Assert.Equal(403, AuthorizationHttpStatus.DenialStatusCode(
            new((AuthorizationOutcome)999, AuthorizationReason.InvalidRequest)));
    }

    private ApiAuthorizationRequirement EventRead =>
        new(new(ResourceKind.Event, _event), ResourceAction.Read, ResourceScope.Event, _event);

    private ApiAuthorizationService Setup(EventRole role = EventRole.EventOrganizer, bool administrator = false)
    {
        _sessions.Session = new(_account, Guid.NewGuid(), Now.AddHours(1));
        _store.Snapshot = new(_account, true, [],
            [new(Guid.NewGuid(), _account, _event, role, Now.AddMinutes(-1))], []);
        _resources.Items[EventRead.Key] = new(EventRead.Key, ResourceScope.Event, _event);
        if (administrator)
            MakeAdministrator();
        var clock = new FixedClock();
        return new(new(_sessions, clock), _store, _resources, clock);
    }

    private void MakeAdministrator() => _store.Snapshot = _store.Snapshot! with
    {
        EventRoles = [],
        GlobalRoles = [new(Guid.NewGuid(), _account, GlobalRole.SystemAdministrator, Now.AddMinutes(-1))]
    };

    private ApiAuthorizationRequirement Add(ResourceKind kind)
    {
        var key = new ResourceKey(kind, Guid.NewGuid());
        _resources.Items[key] = new(key, ResourceScope.Event, _event);
        return new(key, ResourceAction.Read, ResourceScope.Event, _event);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryStore : IAuthorizationStore
    {
        public AuthorizationSnapshot? Snapshot { get; set; }
        public int Reads { get; private set; }
        public bool Fail { get; set; }
        public Task<AuthorizationSnapshot?> GetAsync(Guid accountId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            return Fail ? throw new InvalidOperationException("Synthetic store failure.") : Task.FromResult(Snapshot);
        }
    }

    private sealed class MemoryResources : IAuthorizationResourceResolver
    {
        public Dictionary<ResourceKey, AuthorizationResource?> Items { get; } = [];
        public int Reads { get; private set; }
        public bool Fail { get; set; }
        public Task<AuthorizationResource?> ResolveAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            return Fail ? throw new InvalidOperationException("Synthetic resolver failure.") :
                Task.FromResult(Items.GetValueOrDefault(key));
        }
    }

    private sealed class SessionStub : IAccountSessionRepository
    {
        public ValidAccountSession? Session { get; set; }
        public bool Fail { get; set; }
        public Task<ValidAccountSession?> ValidateAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail)
                throw new InvalidOperationException("Synthetic session failure.");
            return Task.FromResult(tokenHash == Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(Token)))
                ? Session : null);
        }
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
