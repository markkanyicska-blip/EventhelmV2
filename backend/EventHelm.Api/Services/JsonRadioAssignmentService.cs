using System.Text.Json;
using System.Text.Json.Serialization;
using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public sealed class JsonRadioAssignmentService : IRadioAssignmentService
{
    private sealed class Store
    {
        public List<RadioEquipment> Radios { get; set; } = [];
        public List<EventOfficialRadioAssignment> Assignments { get; set; } = [];
    }

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private Store? _store;

    public JsonRadioAssignmentService(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "radio-assignments.json");
    }

    public async Task<IReadOnlyList<RadioEquipment>> GetRadiosAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { return (await LoadAsync(ct)).Radios.OrderBy(x => x.Type).ThenBy(x => x.InventoryNumber).ToList(); }
        finally { _gate.Release(); }
    }

    public async Task<RadioEquipment> CreateRadioAsync(SaveRadioRequest request, CancellationToken ct)
    {
        ValidateRadio(request);
        await _gate.WaitAsync(ct);
        try
        {
            var store = await LoadAsync(ct);
            EnsureInventoryUnique(store, request.InventoryNumber, null);
            var radio = new RadioEquipment
            {
                Type = request.Type,
                InventoryNumber = request.InventoryNumber.Trim(),
                CallSign = request.CallSign.Trim(),
                SerialNumber = Clean(request.SerialNumber),
                Condition = request.Condition.Trim(),
                IsActive = request.IsActive,
                Notes = Clean(request.Notes),
            };
            store.Radios.Add(radio);
            await SaveAsync(store, ct);
            return radio;
        }
        finally { _gate.Release(); }
    }

    public async Task<RadioEquipment?> UpdateRadioAsync(string id, SaveRadioRequest request, CancellationToken ct)
    {
        ValidateRadio(request);
        await _gate.WaitAsync(ct);
        try
        {
            var store = await LoadAsync(ct);
            var radio = store.Radios.FirstOrDefault(x => Eq(x.Id, id));
            if (radio is null) return null;
            EnsureInventoryUnique(store, request.InventoryNumber, id);
            radio.Type = request.Type;
            radio.InventoryNumber = request.InventoryNumber.Trim();
            radio.CallSign = request.CallSign.Trim();
            radio.SerialNumber = Clean(request.SerialNumber);
            radio.Condition = request.Condition.Trim();
            radio.IsActive = request.IsActive;
            radio.Notes = Clean(request.Notes);
            await SaveAsync(store, ct);
            return radio;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<RadioAssignmentView>> GetAssignmentsAsync(string eventId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var store = await LoadAsync(ct);
            return store.Assignments.Where(x => Eq(x.EventId, eventId))
                .Select(x => new RadioAssignmentView(x, FindRadio(store, x.TrunkRadioId), FindRadio(store, x.VhfRadioId)))
                .ToList();
        }
        finally { _gate.Release(); }
    }

    public async Task<EventOfficialRadioAssignment> CreateAssignmentAsync(string eventId, SaveRadioAssignmentRequest request, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var store = await LoadAsync(ct);
            ValidateAssignment(store, eventId, request, null);
            var assignment = new EventOfficialRadioAssignment
            {
                EventId = eventId,
                OfficialId = request.OfficialId,
                Role = Clean(request.Role),
                TrunkRadioId = Clean(request.TrunkRadioId),
                VhfRadioId = Clean(request.VhfRadioId),
                Notes = Clean(request.Notes),
            };
            store.Assignments.Add(assignment);
            await SaveAsync(store, ct);
            return assignment;
        }
        finally { _gate.Release(); }
    }

    public async Task<EventOfficialRadioAssignment?> UpdateAssignmentAsync(string eventId, string assignmentId, SaveRadioAssignmentRequest request, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var store = await LoadAsync(ct);
            var assignment = store.Assignments.FirstOrDefault(x => Eq(x.Id, assignmentId) && Eq(x.EventId, eventId));
            if (assignment is null) return null;
            ValidateAssignment(store, eventId, request, assignmentId);
            assignment.OfficialId = request.OfficialId;
            assignment.Role = Clean(request.Role);
            assignment.TrunkRadioId = Clean(request.TrunkRadioId);
            assignment.VhfRadioId = Clean(request.VhfRadioId);
            assignment.Notes = Clean(request.Notes);
            assignment.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveAsync(store, ct);
            return assignment;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAssignmentAsync(string eventId, string assignmentId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var store = await LoadAsync(ct);
            var assignment = store.Assignments.FirstOrDefault(x => Eq(x.Id, assignmentId) && Eq(x.EventId, eventId));
            if (assignment is null) return false;
            store.Assignments.Remove(assignment);
            await SaveAsync(store, ct);
            return true;
        }
        finally { _gate.Release(); }
    }

    private async Task<Store> LoadAsync(CancellationToken ct)
    {
        if (_store is not null) return _store;
        if (!File.Exists(_path))
        {
            _store = DefaultStore();
            await SaveAsync(_store, ct);
            return _store;
        }
        await using var stream = File.OpenRead(_path);
        _store = await JsonSerializer.DeserializeAsync<Store>(stream, _options, ct) ?? DefaultStore();
        return _store;
    }

    private async Task SaveAsync(Store store, CancellationToken ct)
    {
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, store, _options, ct);
        File.Move(temp, _path, true);
    }

    private static Store DefaultStore() => new()
    {
        Radios =
        [
            new() { Type = RadioEquipmentType.Trunk, InventoryNumber = "TR-001", CallSign = "TRUNK-01" },
            new() { Type = RadioEquipmentType.Trunk, InventoryNumber = "TR-002", CallSign = "TRUNK-02" },
            new() { Type = RadioEquipmentType.Vhf, InventoryNumber = "VHF-001", CallSign = "VHF-01" },
            new() { Type = RadioEquipmentType.Vhf, InventoryNumber = "VHF-002", CallSign = "VHF-02" },
        ]
    };

    private static void ValidateRadio(SaveRadioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InventoryNumber)) throw new ArgumentException("A leltári szám kötelező.");
        if (string.IsNullOrWhiteSpace(request.CallSign)) throw new ArgumentException("A hívójel kötelező.");
        if (string.IsNullOrWhiteSpace(request.Condition)) throw new ArgumentException("A műszaki állapot kötelező.");
    }

    private static void ValidateAssignment(Store store, string eventId, SaveRadioAssignmentRequest request, string? exceptId)
    {
        if (string.IsNullOrWhiteSpace(request.OfficialId)) throw new ArgumentException("A rendező kiválasztása kötelező.");
        if (store.Assignments.Any(x => !Eq(x.Id, exceptId) && Eq(x.EventId, eventId) && Eq(x.OfficialId, request.OfficialId)))
            throw new InvalidOperationException("A rendező ehhez a versenyhez már hozzá van rendelve.");
        ValidateRadioSelection(store, eventId, request.TrunkRadioId, RadioEquipmentType.Trunk, exceptId);
        ValidateRadioSelection(store, eventId, request.VhfRadioId, RadioEquipmentType.Vhf, exceptId);
    }

    private static void ValidateRadioSelection(Store store, string eventId, string? radioId, RadioEquipmentType expectedType, string? exceptId)
    {
        if (string.IsNullOrWhiteSpace(radioId)) return;
        var radio = FindRadio(store, radioId) ?? throw new InvalidOperationException("A kiválasztott rádió nem található.");
        if (!radio.IsActive) throw new InvalidOperationException("Inaktív rádió nem osztható ki.");
        if (radio.Type != expectedType) throw new InvalidOperationException("A rádió típusa nem megfelelő.");
        if (store.Assignments.Any(x => !Eq(x.Id, exceptId) && Eq(x.EventId, eventId) && (Eq(x.TrunkRadioId, radioId) || Eq(x.VhfRadioId, radioId))))
            throw new InvalidOperationException("Ez a rádió ezen a versenyen már ki van osztva.");
    }

    private static void EnsureInventoryUnique(Store store, string number, string? exceptId)
    {
        if (store.Radios.Any(x => !Eq(x.Id, exceptId) && Eq(x.InventoryNumber, number)))
            throw new InvalidOperationException("Ez a leltári szám már létezik.");
    }

    private static RadioEquipment? FindRadio(Store store, string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : store.Radios.FirstOrDefault(x => Eq(x.Id, id));
    private static bool Eq(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
