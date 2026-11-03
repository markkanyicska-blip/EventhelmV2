namespace EventHelm.Api.Models;

public sealed class EventBoat
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid BoatId { get; set; }

    public string? Code { get; set; }

    public string? DisplayName { get; set; }

    public Guid? AreaId { get; set; }

    public Guid? TaskId { get; set; }

    public Guid? ShiftId { get; set; }

    public string? RadioChannel { get; set; }

    public string? TrunkGroup { get; set; }

    public DateTimeOffset? StartAt { get; set; }

    public DateTimeOffset? EndAt { get; set; }

    public string Status { get; set; } = "Planned";

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? BoatName { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? CallSign { get; set; }

    public string? AreaName { get; set; }

    public string? TaskName { get; set; }

    public string? ShiftName { get; set; }
}