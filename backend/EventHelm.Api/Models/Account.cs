using System.Text.Json.Serialization;

namespace EventHelm.Api.Models;

public enum AccountStatus
{
    Pending,
    Active,
    Suspended
}

public sealed record Account
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public AccountStatus Status { get; init; } = AccountStatus.Pending;

    [JsonIgnore]
    public string? PasswordHash { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public long Version { get; init; }

    public override string ToString() => $"Account {{ Id = {Id}, UserId = {UserId}, Status = {Status} }}";
}
