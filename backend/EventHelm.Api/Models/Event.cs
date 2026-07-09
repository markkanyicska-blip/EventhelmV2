
namespace EventHelm.Api.Models;

public class Event
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "Draft";
}