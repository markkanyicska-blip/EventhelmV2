namespace EventHelm.Api.Models;

public sealed class Boat
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string RegistrationNumber { get; set; } = "";

    public string CallSign { get; set; } = "";

    public string? Type { get; set; }

    public int Capacity { get; set; }

    public string? Manufacturer { get; set; }

    public string? Engine { get; set; }

    public int? EnginePower { get; set; }

    public string? FuelType { get; set; }

    public string Status { get; set; } = "Active";

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}