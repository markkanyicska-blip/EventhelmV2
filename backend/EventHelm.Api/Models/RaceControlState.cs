namespace EventHelm.Api.Models;

public sealed class RaceControlState
{
    public string Id { get; set; } = "main";
    public string RaceName { get; set; } = "Balaton Race 07";
    public string Location { get; set; } = "Balatonfüred";
    public RaceStatus Status { get; set; } = RaceStatus.Preparation;
    public string ActiveFlag { get; set; } = "AP";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public WeatherState Weather { get; set; } = new();
    public RadioState Radio { get; set; } = new();
    public List<RaceUnitState> Units { get; set; } = CreateDefaultUnits();
    public List<RaceTimelineEntry> Timeline { get; set; } = new();

    private static List<RaceUnitState> CreateDefaultUnits() =>
    [
        new()
        {
            Id = "rc-01",
            Name = "Alpha",
            CallSign = "RC-01",
            Crew = 3,
            Status = RaceUnitStatus.OnWater,
        },
        new()
        {
            Id = "rc-02",
            Name = "Bravo",
            CallSign = "RC-02",
            Crew = 2,
            Status = RaceUnitStatus.Ready,
        },
        new()
        {
            Id = "rc-03",
            Name = "Charlie",
            CallSign = "RC-03",
            Crew = 2,
            Status = RaceUnitStatus.Standby,
        },
    ];
}

public enum RaceStatus
{
    Preparation,
    Starting,
    InProgress,
    Finished,
}

public sealed class WeatherState
{
    public decimal WindSpeedKnots { get; set; } = 12;
    public decimal GustSpeedKnots { get; set; } = 17;
    public int WindDirectionDegrees { get; set; } = 318;
    public decimal PressureHpa { get; set; } = 1017;
    public decimal WaterTemperatureCelsius { get; set; } = 23.1m;
}

public sealed class RadioState
{
    public string Channel { get; set; } = "VHF 72";
    public bool IsOnline { get; set; } = true;
    public int ActiveCalls { get; set; }
    public string? LastCallSign { get; set; }
    public string? LastMessage { get; set; }
}

public sealed class RaceUnitState
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string CallSign { get; set; }
    public int Crew { get; set; }
    public RaceUnitStatus Status { get; set; } = RaceUnitStatus.Standby;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
}

public enum RaceUnitStatus
{
    Ready,
    OnWater,
    Standby,
    Offline,
}

public sealed class RaceTimelineEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public required string Type { get; set; }
    public required string Message { get; set; }
}
