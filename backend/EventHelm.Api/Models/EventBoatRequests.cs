namespace EventHelm.Api.Models;

public sealed class CreateEventBoatRequest
{
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
}

public sealed class UpdateEventBoatRequest
{
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
}

public sealed class CreateEventBoatCrewRequest
{
    public Guid EventOfficialId { get; set; }

    public string Role { get; set; } = "Crew";

    public bool IsLeader { get; set; }

    public DateTimeOffset? StartAt { get; set; }

    public DateTimeOffset? EndAt { get; set; }

    public string? Notes { get; set; }
}

public sealed class UpdateEventBoatCrewRequest
{
    public Guid EventOfficialId { get; set; }

    public string Role { get; set; } = "Crew";

    public bool IsLeader { get; set; }

    public DateTimeOffset? StartAt { get; set; }

    public DateTimeOffset? EndAt { get; set; }

    public string? Notes { get; set; }
}