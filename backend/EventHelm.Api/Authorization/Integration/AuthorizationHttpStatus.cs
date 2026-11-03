namespace EventHelm.Api.Authorization.Integration;

public static class AuthorizationHttpStatus
{
    // Null means only that authorization passed, not that a business operation succeeded.
    public static int? DenialStatusCode(AuthorizationDecision decision) => decision.Outcome switch
    {
        AuthorizationOutcome.Allowed => null,
        AuthorizationOutcome.Unauthenticated => 401,
        _ => 403
    };
}
