namespace EventHelm.Api.Models;

public enum RadioEquipmentType { Trunk, Vhf }

public sealed class RadioEquipment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public RadioEquipmentType Type { get; set; }
    public string InventoryNumber { get; set; } = "";
    public string CallSign { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string Condition { get; set; } = "Operational";
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public sealed class EventOfficialRadioAssignment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public required string EventId { get; set; }
    public required string OfficialId { get; set; }
    public string? Role { get; set; }
    public string? TrunkRadioId { get; set; }
    public string? VhfRadioId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record RadioAssignmentView(
    EventOfficialRadioAssignment Assignment,
    RadioEquipment? TrunkRadio,
    RadioEquipment? VhfRadio);

public sealed record SaveRadioRequest(
    RadioEquipmentType Type,
    string InventoryNumber,
    string CallSign,
    string? SerialNumber,
    string Condition,
    bool IsActive,
    string? Notes);

public sealed record SaveRadioAssignmentRequest(
    string OfficialId,
    string? Role,
    string? TrunkRadioId,
    string? VhfRadioId,
    string? Notes);
