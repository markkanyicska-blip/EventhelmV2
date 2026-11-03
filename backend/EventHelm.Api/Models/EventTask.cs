namespace EventHelm.Api.Models;

public sealed class EventTask
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public int RequiredOfficials { get; set; }

    public int RequiredBoats { get; set; }

    public string? RequiredQualification { get; set; }

    public string Priority { get; set; } = "Normal";

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CreateEventTaskRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public int RequiredOfficials { get; set; } = 1;

    public int RequiredBoats { get; set; } = 0;

    public string? RequiredQualification { get; set; }

    public string Priority { get; set; } = "Normal";

    public string? Notes { get; set; }
}

public sealed class UpdateEventTaskRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public int RequiredOfficials { get; set; } = 1;

    public int RequiredBoats { get; set; } = 0;

    public string? RequiredQualification { get; set; }

    public string Priority { get; set; } = "Normal";

    public string? Notes { get; set; }
}