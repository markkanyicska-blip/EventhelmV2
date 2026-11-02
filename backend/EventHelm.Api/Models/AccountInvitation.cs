using System.Text.Json.Serialization;

namespace EventHelm.Api.Models;

public sealed record AccountInvitation
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    [JsonIgnore]
    public string TokenHash { get; init; } = "";
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RedeemedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }

    public override string ToString() => $"AccountInvitation {{ Id = {Id}, AccountId = {AccountId} }}";
}

public sealed record IssuedAccountInvitation(
    Guid Id, Guid AccountId, DateTimeOffset ExpiresAt, [property: JsonIgnore] string Token)
{
    public override string ToString() => $"IssuedAccountInvitation {{ Id = {Id}, AccountId = {AccountId} }}";
}

public sealed record AccountAuditEvent(
    Guid Id, Guid AccountId, Guid? InvitationId, string EventType, DateTimeOffset OccurredAt);
