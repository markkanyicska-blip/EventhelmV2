namespace EventHelm.Api.Models;

public class BoatDefect
{
    public Guid Id { get; set; }
    public Guid BoatId { get; set; }
    public DateTime ReportedAt { get; set; }
    public string ReportedBy { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Reported";
    public DateTime CreatedAt { get; set; }
}
