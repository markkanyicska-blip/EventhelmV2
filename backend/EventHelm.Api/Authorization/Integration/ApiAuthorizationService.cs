using EventHelm.Api.Services;

namespace EventHelm.Api.Authorization.Integration;

// Dormant: no registration, HTTP context, cookies, claims, business reads or writes.
public sealed class ApiAuthorizationService(
    AccountSessionService sessions, IAuthorizationStore store,
    IAuthorizationResourceResolver resources, TimeProvider clock)
{
    public async Task<AuthorizationDecision> AuthorizeAllAsync(
        string? individualSessionToken,
        IReadOnlyList<ApiAuthorizationRequirement?>? requirements,
        CancellationToken cancellationToken = default)
    {
        if (await sessions.ValidateAsync(individualSessionToken, cancellationToken) is null)
            return new(AuthorizationOutcome.Unauthenticated, AuthorizationReason.InvalidSession);

        if (requirements is null || requirements.Count == 0)
            return DenyInvalidRequest();

        // Keep operation metadata stable during resource checks; a failed child
        // must never fall back to a parent/event permission.
        var checks = requirements.ToArray();
        if (checks.Any(check => !IsValid(check)))
            return DenyInvalidRequest();

        var administratorOverride = false;
        foreach (var check in checks)
        {
            var requirement = check!;
            var authorization = new EventAuthorizationService(
                sessions, store, new BoundaryResourceResolver(resources, requirement), clock);
            var decision = await authorization.AuthorizeAsync(
                individualSessionToken, requirement.Key, requirement.Action,
                cancellationToken: cancellationToken);
            if (!decision.IsAllowed)
                return decision;
            administratorOverride |= decision.Reason == AuthorizationReason.AdministratorOverride;
        }

        return new(AuthorizationOutcome.Allowed, administratorOverride
            ? AuthorizationReason.AdministratorOverride : AuthorizationReason.Granted);
    }

    private static bool IsValid(ApiAuthorizationRequirement? requirement) =>
        requirement is { Key: { } key } && key.Id != Guid.Empty &&
        Enum.IsDefined(key.Kind) && Enum.IsDefined(requirement.Action) &&
        RolePermissions.For(key.Kind, requirement.Action) is not null &&
        requirement.Scope switch
        {
            ResourceScope.Event => requirement.ExpectedEventId is { } eventId && eventId != Guid.Empty &&
                !IsGlobalKey(key),
            ResourceScope.Global => requirement.ExpectedEventId is null && IsGlobalKey(key),
            _ => false
        };

    private static bool IsGlobalKey(ResourceKey key) =>
        key == AuthorizationResourceKeys.System || key == AuthorizationResourceKeys.EventCreation ||
        key == AuthorizationResourceKeys.GlobalInventory || key == AuthorizationResourceKeys.GlobalRaceControl;

    private static AuthorizationDecision DenyInvalidRequest() =>
        new(AuthorizationOutcome.Forbidden, AuthorizationReason.InvalidRequest);

    private sealed class BoundaryResourceResolver(
        IAuthorizationResourceResolver inner, ApiAuthorizationRequirement requirement)
        : IAuthorizationResourceResolver
    {
        public async Task<AuthorizationResource?> ResolveAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            var resource = await inner.ResolveAsync(key, cancellationToken);
            if (resource is null || resource.Key != key || resource.Scope != requirement.Scope)
                return null;

            return requirement.Scope switch
            {
                ResourceScope.Event when resource.EventId == requirement.ExpectedEventId => resource,
                ResourceScope.Global when resource.EventId is null => resource,
                _ => null
            };
        }
    }
}
