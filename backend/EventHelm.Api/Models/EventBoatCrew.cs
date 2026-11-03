namespace EventHelm.Api.Models;

public sealed class EventBoatCrew
{
    public Guid Id { get; set; }

    public Guid EventBoatId { get; set; }

    public Guid EventOfficialId { get; set; }

    public string Role { get; set; } = "Crew";

    public bool IsLeader { get; set; }

    public DateTimeOffset? StartAt { get; set; }

    public DateTimeOffset? EndAt { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid OfficialId { get; set; }

    public string? OfficialName { get; set; }

    public string? OfficialPhone { get; set; }

    public string? OfficialEmail { get; set; }
}