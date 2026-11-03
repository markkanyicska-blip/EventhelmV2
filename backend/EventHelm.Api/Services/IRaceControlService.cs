using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public interface IRaceControlService
{
    Task<RaceControlState> GetAsync(CancellationToken cancellationToken);
    Task<RaceControlState> UpdateStatusAsync(
        RaceStatus status,
        CancellationToken cancellationToken);
    Task<RaceControlState> UpdateFlagAsync(
        string flag,
        CancellationToken cancellationToken);
    Task<RaceControlState> UpdateWeatherAsync(
        WeatherState weather,
        CancellationToken cancellationToken);
    Task<RaceControlState> UpdateRadioAsync(
        RadioState radio,
        CancellationToken cancellationToken);
    Task<RaceControlState?> UpdateUnitAsync(
        string id,
        UpdateRaceUnitRequest request,
        CancellationToken cancellationToken);
    Task<RaceControlState> AddTimelineEntryAsync(
        AddTimelineEntryRequest request,
        CancellationToken cancellationToken);
}
