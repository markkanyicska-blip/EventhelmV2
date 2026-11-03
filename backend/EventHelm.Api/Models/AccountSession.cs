using System.Text.Json.Serialization;

namespace EventHelm.Api.Models;

public sealed record AccountLogin(Account Account, long SecurityVersion);

public sealed record IssuedAccountSession(
    Guid AccountId, DateTimeOffset ExpiresAt, [property: JsonIgnore] string Token)
{
    public override string ToString() => $"IssuedAccountSession {{ AccountId = {AccountId}, ExpiresAt = {ExpiresAt} }}";
}

public sealed record ValidAccountSession(Guid AccountId, Guid UserId, DateTimeOffset ExpiresAt);
