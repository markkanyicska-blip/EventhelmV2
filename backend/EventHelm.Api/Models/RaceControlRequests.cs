namespace EventHelm.Api.Models;

public sealed record UpdateRaceStatusRequest(RaceStatus Status);

public sealed record UpdateRaceFlagRequest(string Flag);

public sealed record UpdateWeatherRequest(
    decimal WindSpeedKnots,
    decimal GustSpeedKnots,
    int WindDirectionDegrees,
    decimal PressureHpa,
    decimal WaterTemperatureCelsius);

public sealed record UpdateRadioRequest(
    string Channel,
    bool IsOnline,
    int ActiveCalls,
    string? LastCallSign,
    string? LastMessage);

public sealed record UpdateRaceUnitRequest(
    RaceUnitStatus Status,
    decimal? Latitude,
    decimal? Longitude);

public sealed record AddTimelineEntryRequest(
    string Type,
    string Message);
