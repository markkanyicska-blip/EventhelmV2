namespace EventHelm.Api.Authorization;

public enum GlobalRole { SystemAdministrator = 1 }
public enum EventRole { EventOrganizer = 1, RaceOfficer = 2, TechnicalStaff = 3, OnWaterPersonnel = 4 }

public enum ResourceKind
{
    System = 1, Event, Officials, Boats, Assignments, Shifts, Tasks,
    Communications, Documents, RaceControl, PersonalInformation, MedicalInformation,
    EventRoles, GlobalInventory
}

public enum ResourceAction
{
    Read = 1, Create, Manage, Plan, Allocate, Report, Resolve, UpdateOwn, Operate, GrantRole
}

public enum Permission
{
    ManageSystem = 1, CreateEvent, ReadEvent, ManageEvent, PlanEvent,
    ReadOfficials, ManageOfficials, ReadBoats, AllocateBoats, ReportDefect, ResolveDefect,
    ReadAssignments, ManageAssignments, ReadShifts, ManageShifts,
    ReadTasks, ManageTasks, UpdateOwnTask, ReadCommunications, SendCommunications,
    ReadDocuments, ManageDocuments, ReadRaceControl, OperateRaceControl,
    ReadPersonalInformation = 101, ManagePersonalInformation, ReadMedicalInformation,
    ManageMedicalInformation, ReadPrivateDocuments, ManagePrivateDocuments,
    GrantEventRole = 201, ManageGlobalInventory, UpdateOwnUnit
}

public enum ResourceScope { Event = 1, Global }
public enum DocumentStorage { NotApplicable, Private, PublicStatic }
public enum AuthorizationOutcome { Allowed, Unauthenticated, Forbidden }
public enum AuthorizationReason
{
    Granted, AdministratorOverride, InvalidSession, InvalidRequest, InactiveAccount, MissingResourceScope,
    MissingMembership, MissingPermission, MissingSensitiveGrant, UnsafeDocumentStorage,
    PrivilegeEscalation
}

public sealed record ResourceKey(ResourceKind Kind, Guid Id);

public static class AuthorizationResourceKeys
{
    // Explicit global collection/singleton identifiers; never accept an event ID as global scope.
    public static readonly ResourceKey System = new(ResourceKind.System, new("00000000-0000-0000-0000-000000000001"));
    public static readonly ResourceKey EventCreation = new(ResourceKind.Event, new("00000000-0000-0000-0000-000000000002"));
    public static readonly ResourceKey GlobalInventory = new(ResourceKind.GlobalInventory, new("00000000-0000-0000-0000-000000000003"));
    public static readonly ResourceKey GlobalRaceControl = new(ResourceKind.RaceControl, new("00000000-0000-0000-0000-000000000004"));
}

// Only a trusted server-side resolver may construct this context, never model binding.
// AssignedAccountId must come from an explicit account-to-resource relationship.
public sealed record AuthorizationResource(
    ResourceKey Key, ResourceScope Scope, Guid? EventId = null,
    Guid? AssignedAccountId = null, bool IsPrivateDocument = false,
    DocumentStorage DocumentStorage = DocumentStorage.NotApplicable);

public sealed record EventRoleAssignment(
    Guid Id, Guid AccountId, Guid EventId, EventRole Role,
    DateTimeOffset GrantedAt, DateTimeOffset? ExpiresAt = null, DateTimeOffset? RevokedAt = null)
{
    public bool IsActive(DateTimeOffset now) =>
        GrantedAt <= now && (ExpiresAt is null || ExpiresAt > now) && RevokedAt is null;
}

public sealed record GlobalRoleAssignment(
    Guid Id, Guid AccountId, GlobalRole Role,
    DateTimeOffset GrantedAt, DateTimeOffset? ExpiresAt = null, DateTimeOffset? RevokedAt = null)
{
    public bool IsActive(DateTimeOffset now) =>
        GrantedAt <= now && (ExpiresAt is null || ExpiresAt > now) && RevokedAt is null;
}

public sealed record SensitivePermissionGrant(
    Guid Id, Guid AccountId, Guid EventId, Permission Permission, string Purpose,
    DateTimeOffset GrantedAt, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt = null)
{
    public bool IsActive(DateTimeOffset now) =>
        GrantedAt <= now && ExpiresAt > now && RevokedAt is null && !string.IsNullOrWhiteSpace(Purpose);
}

public sealed record AuthorizationSnapshot(
    Guid AccountId, bool IsActive, IReadOnlyList<GlobalRoleAssignment> GlobalRoles,
    IReadOnlyList<EventRoleAssignment> EventRoles, IReadOnlyList<SensitivePermissionGrant> SensitiveGrants);

public sealed record AuthorizationDecision(AuthorizationOutcome Outcome, AuthorizationReason Reason)
{
    public bool IsAllowed => Outcome == AuthorizationOutcome.Allowed;
}

public interface IAuthorizationStore
{
    // Must read current account/user eligibility and grants, never claims or Users.Role.
    Task<AuthorizationSnapshot?> GetAsync(Guid accountId, CancellationToken cancellationToken);
}

public interface IAuthorizationResourceResolver
{
    // Must verify existence, canonical kind/classification, event ownership and assignment.
    // Unknown/unscoped resources return null; do not infer scope from the route alone.
    Task<AuthorizationResource?> ResolveAsync(ResourceKey key, CancellationToken cancellationToken);
}
