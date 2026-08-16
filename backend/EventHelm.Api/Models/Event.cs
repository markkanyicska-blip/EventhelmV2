namespace EventHelm.Api.Models;

public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Draft";

    public string? OrganizingClub { get; set; }
    public string? HostingClub { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public string? WaterArea { get; set; }

    public string? RaceOfficer { get; set; }
    public string? JuryChair { get; set; }
    public string? TechnicalCommitteeChair { get; set; }
    public string? RaceOffice { get; set; }

    public string? VhfChannel { get; set; }
    public string? TrunkGroup { get; set; }
    public string? Notes { get; set; }
}
