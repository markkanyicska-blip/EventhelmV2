using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/assignments")]
public sealed class EventAssignmentsController : ControllerBase
{
    private readonly EventAssignmentService _service;
    private readonly ILogger<EventAssignmentsController> _logger;

    public EventAssignmentsController(
        EventAssignmentService service,
        ILogger<EventAssignmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<EventAssignment>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventAssignment>>> GetAll(
        Guid eventId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A rendezvény azonosítója kötelező."
            });
        }

        if (from.HasValue &&
            to.HasValue &&
            to.Value <= from.Value)
        {
            return BadRequest(new
            {
                message =
                    "A lekérdezés záró időpontjának későbbinek kell lennie a kezdő időpontnál."
            });
        }

        var assignments =
            await _service.GetAllAsync(
                eventId,
                from,
                to,
                cancellationToken);

        return Ok(assignments);
    }

    [HttpGet("{assignmentId:guid}")]
    [ProducesResponseType(
        typeof(EventAssignment),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventAssignment>> GetById(
        Guid eventId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty ||
            assignmentId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "A rendezvény és a beosztás azonosítója kötelező."
            });
        }

        var assignment =
            await _service.GetByIdAsync(
                eventId,
                assignmentId,
                cancellationToken);

        if (assignment is null)
        {
            return NotFound(new
            {
                message = "A beosztás nem található."
            });
        }

        return Ok(assignment);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(EventAssignment),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventAssignment>> Create(
        Guid eventId,
        [FromBody] CreateEventAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "A kérés tartalma kötelező."
            });
        }

        try
        {
            var assignment =
                await _service.CreateAsync(
                    eventId,
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    eventId,
                    assignmentId = assignment.Id
                },
                assignment);
        }
        catch (AssignmentValidationException exception)
        {
            return Conflict(new
            {
                message = exception.Message,
                validation = exception.Validation
            });
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt a beosztás létrehozásakor. EventId: {EventId}",
                eventId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a beosztás létrehozásakor."
                });
        }
    }

    [HttpPut("{assignmentId:guid}")]
    [ProducesResponseType(
        typeof(EventAssignment),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventAssignment>> Update(
        Guid eventId,
        Guid assignmentId,
        [FromBody] UpdateEventAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "A kérés tartalma kötelező."
            });
        }

        if (assignmentId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A beosztás azonosítója kötelező."
            });
        }

        try
        {
            var assignment =
                await _service.UpdateAsync(
                    eventId,
                    assignmentId,
                    request,
                    cancellationToken);

            if (assignment is null)
            {
                return NotFound(new
                {
                    message = "A beosztás nem található."
                });
            }

            return Ok(assignment);
        }
        catch (AssignmentValidationException exception)
        {
            return Conflict(new
            {
                message = exception.Message,
                validation = exception.Validation
            });
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt a beosztás módosításakor. EventId: {EventId}, AssignmentId: {AssignmentId}",
                eventId,
                assignmentId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a beosztás módosításakor."
                });
        }
    }

    [HttpDelete("{assignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid eventId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty ||
            assignmentId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "A rendezvény és a beosztás azonosítója kötelező."
            });
        }

        try
        {
            var deleted =
                await _service.DeleteAsync(
                    eventId,
                    assignmentId,
                    cancellationToken);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "A beosztás nem található."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt a beosztás törlésekor. EventId: {EventId}, AssignmentId: {AssignmentId}",
                eventId,
                assignmentId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a beosztás törlésekor."
                });
        }
    }

    [HttpPost("validate")]
    [ProducesResponseType(
        typeof(AssignmentValidationResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentValidationResult>> Validate(
        Guid eventId,
        [FromBody] ValidateEventAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "A kérés tartalma kötelező."
            });
        }

        try
        {
            var validation =
                await _service.ValidateAsync(
                    eventId,
                    request,
                    cancellationToken);

            return Ok(validation);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Hiba történt a beosztás ellenőrzésekor. EventId: {EventId}",
                eventId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Váratlan hiba történt a beosztás ellenőrzésekor."
                });
        }
    }
}