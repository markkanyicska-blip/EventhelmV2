namespace EventHelm.Api.Models;

public sealed class BoatCrewAssignment
{
    public Guid Id { get; set; }

    public Guid BoatId { get; set; }

    public Guid OfficialId { get; set; }

    public string Role { get; set; } = "Rendező";

    public bool IsCaptain { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public string OfficialName { get; set; } = "";

    public string? OfficialPhone { get; set; }

    public string? OfficialEmail { get; set; }

    public string? OfficialProfileImageUrl { get; set; }
}

public sealed class CreateBoatCrewAssignmentRequest
{
    public Guid OfficialId { get; set; }

    public string Role { get; set; } = "Rendező";

    public bool IsCaptain { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Notes { get; set; }
}

public sealed class UpdateBoatCrewAssignmentRequest
{
    public string Role { get; set; } = "Rendező";

    public bool IsCaptain { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Notes { get; set; }
}