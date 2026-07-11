namespace EventHelm.Api.Models;

public class Boat
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = string.Empty;

    public string CallSign { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}