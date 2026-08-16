namespace EventHelm.Api.Models;

public sealed class EventOfficialAssignment
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid OfficialId { get; set; }
    public string Role { get; set; } = "";
    public string? GroupName { get; set; }
    public string? SubgroupName { get; set; }
    public string? Task { get; set; }
    public int AssignmentOrder { get; set; }
    public string? TrunkRadioNumber { get; set; }
    public string? VhfRadioNumber { get; set; }
    public Guid? BoatId { get; set; }
    public string AttendanceStatus { get; set; } = "Planned";
    public string? Notes { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class EventOfficialAssignmentDetails
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid OfficialId { get; set; }
    public string OfficialName { get; set; } = "";
    public string? OfficialPhone { get; set; }
    public string? OfficialEmail { get; set; }
    public string Role { get; set; } = "";
    public string? GroupName { get; set; }
    public string? SubgroupName { get; set; }
    public string? Task { get; set; }
    public int AssignmentOrder { get; set; }
    public string? TrunkRadioNumber { get; set; }
    public string? VhfRadioNumber { get; set; }
    public Guid? BoatId { get; set; }
    public string? BoatName { get; set; }
    public string? BoatCallSign { get; set; }
    public string AttendanceStatus { get; set; } = "Planned";
    public string? Notes { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed record SaveEventOfficialAssignmentRequest(
    Guid OfficialId,
    string Role,
    string? GroupName,
    string? SubgroupName,
    string? Task,
    int AssignmentOrder,
    string? TrunkRadioNumber,
    string? VhfRadioNumber,
    Guid? BoatId,
    string AttendanceStatus,
    string? Notes);
