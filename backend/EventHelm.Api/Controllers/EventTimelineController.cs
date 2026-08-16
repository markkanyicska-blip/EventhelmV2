using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/timeline")]
public sealed class EventTimelineController : ControllerBase
{
    private readonly EventTimelineService _service;
    private readonly ILogger<EventTimelineController> _logger;

    public EventTimelineController(
        EventTimelineService service,
        ILogger<EventTimelineController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(EventTimelineResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTimelineResult>> GetTimeline(
        Guid eventId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var timeline =
                await _service.GetTimelineAsync(
                    eventId,
                    from,
                    to,
                    cancellationToken);

            return Ok(timeline);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt a teljes timeline lekérdezésekor. EventId: {EventId}",
                eventId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a timeline lekérdezésekor."
                });
        }
    }

    [HttpGet("officials")]
    [ProducesResponseType(
        typeof(IReadOnlyList<OfficialTimelineRow>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OfficialTimelineRow>>>
        GetOfficialsTimeline(
            Guid eventId,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            CancellationToken cancellationToken)
    {
        try
        {
            var timeline =
                await _service.GetOfficialsTimelineAsync(
                    eventId,
                    from,
                    to,
                    cancellationToken);

            return Ok(timeline);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt a rendezői timeline lekérdezésekor. EventId: {EventId}",
                eventId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a rendezői timeline lekérdezésekor."
                });
        }
    }

    [HttpGet("officials/{officialId:guid}")]
    [ProducesResponseType(
        typeof(OfficialTimelineRow),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfficialTimelineRow>>
        GetOfficialTimeline(
            Guid eventId,
            Guid officialId,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            CancellationToken cancellationToken)
    {
        try
        {
            var timeline =
                await _service.GetOfficialTimelineAsync(
                    eventId,
                    officialId,
                    from,
                    to,
                    cancellationToken);

            if (timeline is null)
            {
                return NotFound(new
                {
                    message =
                        "A rendezőhöz nem található timeline-bejegyzés a megadott időszakban."
                });
            }

            return Ok(timeline);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt egy rendező timeline-jának lekérdezésekor. EventId: {EventId}, OfficialId: {OfficialId}",
                eventId,
                officialId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a rendező timeline-jának lekérdezésekor."
                });
        }
    }

    [HttpGet("boats")]
    [ProducesResponseType(
        typeof(IReadOnlyList<BoatTimelineRow>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BoatTimelineRow>>>
        GetBoatsTimeline(
            Guid eventId,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            CancellationToken cancellationToken)
    {
        try
        {
            var timeline =
                await _service.GetBoatsTimelineAsync(
                    eventId,
                    from,
                    to,
                    cancellationToken);

            return Ok(timeline);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt a hajós timeline lekérdezésekor. EventId: {EventId}",
                eventId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a hajós timeline lekérdezésekor."
                });
        }
    }

    [HttpGet("boats/{eventBoatId:guid}")]
    [ProducesResponseType(
        typeof(BoatTimelineRow),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoatTimelineRow>>
        GetBoatTimeline(
            Guid eventId,
            Guid eventBoatId,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            CancellationToken cancellationToken)
    {
        try
        {
            var timeline =
                await _service.GetBoatTimelineAsync(
                    eventId,
                    eventBoatId,
                    from,
                    to,
                    cancellationToken);

            if (timeline is null)
            {
                return NotFound(new
                {
                    message =
                        "A hajóhoz nem található timeline-bejegyzés a megadott időszakban."
                });
            }

            return Ok(timeline);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt egy hajó timeline-jának lekérdezésekor. EventId: {EventId}, EventBoatId: {EventBoatId}",
                eventId,
                eventBoatId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a hajó timeline-jának lekérdezésekor."
                });
        }
    }
}