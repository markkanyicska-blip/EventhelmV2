namespace EventHelm.Api.Authorization.Integration;

// Server-defined operation metadata, never an HTTP/model-binding DTO.
// ExpectedEventId is a consistency assertion, not an authoritative resource scope.
public sealed record ApiAuthorizationRequirement(
    ResourceKey Key, ResourceAction Action, ResourceScope Scope, Guid? ExpectedEventId = null);
