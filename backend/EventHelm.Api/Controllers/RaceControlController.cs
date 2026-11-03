using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/race-control")]
public sealed class RaceControlController : ControllerBase
{
    private readonly IRaceControlService _service;

    public RaceControlController(
        IRaceControlService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType<RaceControlState>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<RaceControlState>> Get(
        CancellationToken cancellationToken)
    {
        var state = await _service.GetAsync(
            cancellationToken);

        return Ok(state);
    }

    [HttpPut("status")]
    public async Task<ActionResult<RaceControlState>> UpdateStatus(
        UpdateRaceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var state = await _service.UpdateStatusAsync(
            request.Status,
            cancellationToken);

        return Ok(state);
    }

    [HttpPut("flag")]
    public async Task<ActionResult<RaceControlState>> UpdateFlag(
        UpdateRaceFlagRequest request,
        CancellationToken cancellationToken)
    {
        var state = await _service.UpdateFlagAsync(
            request.Flag,
            cancellationToken);

        return Ok(state);
    }

    [HttpPut("weather")]
    public async Task<ActionResult<RaceControlState>> UpdateWeather(
        UpdateWeatherRequest request,
        CancellationToken cancellationToken)
    {
        var state = await _service.UpdateWeatherAsync(
            new WeatherState
            {
                WindSpeedKnots = request.WindSpeedKnots,
                GustSpeedKnots = request.GustSpeedKnots,
                WindDirectionDegrees =
                    request.WindDirectionDegrees,
                PressureHpa = request.PressureHpa,
                WaterTemperatureCelsius =
                    request.WaterTemperatureCelsius,
            },
            cancellationToken);

        return Ok(state);
    }

    [HttpPut("radio")]
    public async Task<ActionResult<RaceControlState>> UpdateRadio(
        UpdateRadioRequest request,
        CancellationToken cancellationToken)
    {
        var state = await _service.UpdateRadioAsync(
            new RadioState
            {
                Channel = request.Channel,
                IsOnline = request.IsOnline,
                ActiveCalls = request.ActiveCalls,
                LastCallSign = request.LastCallSign,
                LastMessage = request.LastMessage,
            },
            cancellationToken);

        return Ok(state);
    }

    [HttpPut("units/{id}")]
    public async Task<ActionResult<RaceControlState>> UpdateUnit(
        string id,
        UpdateRaceUnitRequest request,
        CancellationToken cancellationToken)
    {
        var state = await _service.UpdateUnitAsync(
            id,
            request,
            cancellationToken);

        return state is null
            ? NotFound()
            : Ok(state);
    }

    [HttpPost("timeline")]
    public async Task<ActionResult<RaceControlState>> AddTimelineEntry(
        AddTimelineEntryRequest request,
        CancellationToken cancellationToken)
    {
        var state =
            await _service.AddTimelineEntryAsync(
                request,
                cancellationToken);

        return Ok(state);
    }
}
