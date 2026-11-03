using EventHelm.Api.Services;

namespace EventHelm.Api.Authorization;

// Dormant: no DI registration, policies, HTTP endpoints, or decision caching.
public sealed class EventAuthorizationService(
    AccountSessionService sessions, IAuthorizationStore store,
    IAuthorizationResourceResolver resources, TimeProvider clock)
{
    public async Task<AuthorizationDecision> AuthorizeAsync(
        string? individualSessionToken, ResourceKey? key, ResourceAction action,
        Guid? targetAccountId = null, EventRole? targetRole = null,
        CancellationToken cancellationToken = default)
    {
        var session = await sessions.ValidateAsync(individualSessionToken, cancellationToken);
        if (session is null)
            return new(AuthorizationOutcome.Unauthenticated, AuthorizationReason.InvalidSession);
        if (key is null || key.Id == Guid.Empty || !Enum.IsDefined(key.Kind) || !Enum.IsDefined(action) ||
            RolePermissions.For(key.Kind, action) is not { } permission)
            return Deny(AuthorizationReason.InvalidRequest);

        var snapshot = await store.GetAsync(session.AccountId, cancellationToken);
        if (snapshot is null || snapshot.AccountId != session.AccountId || !snapshot.IsActive)
            return Deny(AuthorizationReason.InactiveAccount);
        var resource = await resources.ResolveAsync(key, cancellationToken);
        if (resource is null || resource.Key != key || !Enum.IsDefined(resource.Scope))
            return Deny(AuthorizationReason.MissingResourceScope);

        var now = clock.GetUtcNow();
        var administrator = snapshot.GlobalRoles.Any(r => r.AccountId == session.AccountId &&
            r.Role == GlobalRole.SystemAdministrator && r.IsActive(now));
        if (resource.Scope == ResourceScope.Global)
        {
            // Event creation uses a trusted global collection resource, not a fictional event.
            if (resource.EventId is not null ||
                !(key.Kind is ResourceKind.System or ResourceKind.GlobalInventory or ResourceKind.RaceControl ||
                    key.Kind == ResourceKind.Event && action == ResourceAction.Create))
                return Deny(AuthorizationReason.MissingResourceScope);
            return administrator ? AllowAdministrator() : Deny(AuthorizationReason.MissingPermission);
        }

        if (resource.EventId is not { } eventId || eventId == Guid.Empty ||
            key.Kind is ResourceKind.System or ResourceKind.GlobalInventory ||
            action == ResourceAction.Create ||
            key.Kind == ResourceKind.Event && key.Id != eventId)
            return Deny(AuthorizationReason.MissingResourceScope);

        var roles = snapshot.EventRoles.Where(r => r.AccountId == session.AccountId &&
            r.EventId == eventId && Enum.IsDefined(r.Role) && r.IsActive(now)).Select(r => r.Role).ToArray();
        if (!administrator && roles.Length == 0)
            return Deny(AuthorizationReason.MissingMembership);

        if (key.Kind == ResourceKind.Documents &&
            resource.DocumentStorage != DocumentStorage.Private)
            return Deny(AuthorizationReason.UnsafeDocumentStorage);

        if (permission == Permission.GrantEventRole)
        {
            if (targetAccountId is null || targetAccountId == Guid.Empty || targetRole is null ||
                !Enum.IsDefined(targetRole.Value) ||
                !administrator && (targetAccountId == session.AccountId || targetRole == EventRole.EventOrganizer))
                return Deny(AuthorizationReason.PrivilegeEscalation);
            var target = await store.GetAsync(targetAccountId.Value, cancellationToken);
            if (target is null || target.AccountId != targetAccountId || !target.IsActive)
                return Deny(AuthorizationReason.InactiveAccount);
        }

        var sensitivePermission = resource.IsPrivateDocument && key.Kind == ResourceKind.Documents
            ? action == ResourceAction.Read ? Permission.ReadPrivateDocuments : Permission.ManagePrivateDocuments
            : permission;
        if (RolePermissions.IsSensitive(sensitivePermission))
        {
            return snapshot.SensitiveGrants.Any(g => g.AccountId == session.AccountId &&
                g.EventId == eventId && g.Permission == sensitivePermission && g.IsActive(now))
                ? Allow() : Deny(AuthorizationReason.MissingSensitiveGrant);
        }

        if (administrator)
            return AllowAdministrator();
        // Limited roles must prove an explicit account assignment for every allowed subset.
        // A broad role can satisfy the permission independently of a limited role.
        return roles.Any(role => RolePermissions.Allows(role, permission) &&
            (role is EventRole.EventOrganizer or EventRole.RaceOfficer ||
                resource.AssignedAccountId == session.AccountId))
            ? Allow() : Deny(AuthorizationReason.MissingPermission);
    }

    private static AuthorizationDecision Allow() =>
        new(AuthorizationOutcome.Allowed, AuthorizationReason.Granted);

    private static AuthorizationDecision AllowAdministrator() =>
        new(AuthorizationOutcome.Allowed, AuthorizationReason.AdministratorOverride);

    private static AuthorizationDecision Deny(AuthorizationReason reason) =>
        new(AuthorizationOutcome.Forbidden, reason);
}
