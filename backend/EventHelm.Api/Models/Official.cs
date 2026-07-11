namespace EventHelm.Api.Models;

public class Official
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string Role { get; set; } = "Rendező";

    public string Status { get; set; } = "Available";

    public DateTime CreatedAt { get; set; }
}