using System.Text.Json;
using System.Text.Json.Serialization;
using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public sealed class JsonRaceControlService : IRaceControlService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private RaceControlState? _state;

    public JsonRaceControlService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(
            environment.ContentRootPath,
            "App_Data");

        Directory.CreateDirectory(dataDirectory);

        _filePath = Path.Combine(
            dataDirectory,
            "race-control.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter(),
            },
        };
    }

    public async Task<RaceControlState> GetAsync(
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            return await LoadUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<RaceControlState> UpdateStatusAsync(
        RaceStatus status,
        CancellationToken cancellationToken) =>
        MutateAsync(
            state =>
            {
                state.Status = status;
                AddTimelineUnsafe(
                    state,
                    "status",
                    $"Versenystátusz: {status}");
            },
            cancellationToken);

    public Task<RaceControlState> UpdateFlagAsync(
        string flag,
        CancellationToken cancellationToken)
    {
        var normalizedFlag = flag.Trim();

        if (string.IsNullOrWhiteSpace(normalizedFlag))
        {
            throw new ArgumentException(
                "A zászló megadása kötelező.",
                nameof(flag));
        }

        return MutateAsync(
            state =>
            {
                state.ActiveFlag = normalizedFlag;
                AddTimelineUnsafe(
                    state,
                    "flag",
                    $"Aktív jelzés: {normalizedFlag}");
            },
            cancellationToken);
    }

    public Task<RaceControlState> UpdateWeatherAsync(
        WeatherState weather,
        CancellationToken cancellationToken) =>
        MutateAsync(
            state =>
            {
                state.Weather = weather;
                AddTimelineUnsafe(
                    state,
                    "weather",
                    $"Szél frissítve: {weather.WindSpeedKnots} kn / {weather.WindDirectionDegrees}°");
            },
            cancellationToken);

    public Task<RaceControlState> UpdateRadioAsync(
        RadioState radio,
        CancellationToken cancellationToken) =>
        MutateAsync(
            state =>
            {
                state.Radio = radio;
                AddTimelineUnsafe(
                    state,
                    "radio",
                    $"Rádióállapot: {radio.Channel} / {(radio.IsOnline ? "online" : "offline")}");
            },
            cancellationToken);

    public async Task<RaceControlState?> UpdateUnitAsync(
        string id,
        UpdateRaceUnitRequest request,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await LoadUnsafeAsync(cancellationToken);
            var unit = state.Units.FirstOrDefault(
                item => string.Equals(
                    item.Id,
                    id,
                    StringComparison.OrdinalIgnoreCase));

            if (unit is null)
            {
                return null;
            }

            unit.Status = request.Status;
            unit.Latitude = request.Latitude;
            unit.Longitude = request.Longitude;
            unit.LastSeenAt = DateTimeOffset.UtcNow;

            state.UpdatedAt = DateTimeOffset.UtcNow;

            AddTimelineUnsafe(
                state,
                "unit",
                $"{unit.CallSign}: {unit.Status}");

            await SaveUnsafeAsync(
                state,
                cancellationToken);

            return state;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<RaceControlState> AddTimelineEntryAsync(
        AddTimelineEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new ArgumentException(
                "A bejegyzés típusa kötelező.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "A bejegyzés szövege kötelező.");
        }

        return MutateAsync(
            state =>
            {
                AddTimelineUnsafe(
                    state,
                    request.Type.Trim(),
                    request.Message.Trim());
            },
            cancellationToken);
    }

    private async Task<RaceControlState> MutateAsync(
        Action<RaceControlState> mutation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await LoadUnsafeAsync(cancellationToken);

            mutation(state);
            state.UpdatedAt = DateTimeOffset.UtcNow;

            await SaveUnsafeAsync(
                state,
                cancellationToken);

            return state;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<RaceControlState> LoadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (_state is not null)
        {
            return _state;
        }

        if (!File.Exists(_filePath))
        {
            _state = new RaceControlState();

            await SaveUnsafeAsync(
                _state,
                cancellationToken);

            return _state;
        }

        await using var stream = File.OpenRead(_filePath);

        _state =
            await JsonSerializer.DeserializeAsync<RaceControlState>(
                stream,
                _jsonOptions,
                cancellationToken)
            ?? new RaceControlState();

        return _state;
    }

    private async Task SaveUnsafeAsync(
        RaceControlState state,
        CancellationToken cancellationToken)
    {
        var temporaryPath = $"{_filePath}.tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                state,
                _jsonOptions,
                cancellationToken);
        }

        File.Move(
            temporaryPath,
            _filePath,
            overwrite: true);
    }

    private static void AddTimelineUnsafe(
        RaceControlState state,
        string type,
        string message)
    {
        state.Timeline.Insert(
            0,
            new RaceTimelineEntry
            {
                Type = type,
                Message = message,
            });

        if (state.Timeline.Count > 100)
        {
            state.Timeline.RemoveRange(
                100,
                state.Timeline.Count - 100);
        }
    }
}
