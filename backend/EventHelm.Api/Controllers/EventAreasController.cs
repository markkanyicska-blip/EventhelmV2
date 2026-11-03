using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/areas")]
public sealed class EventAreasController : ControllerBase
{
    private readonly EventAreaService _service;

    public EventAreasController(
        EventAreaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventArea>>> GetAll(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var areas =
            await _service.GetAllAsync(
                eventId,
                cancellationToken);

        return Ok(areas);
    }

    [HttpGet("{areaId:guid}")]
    public async Task<ActionResult<EventArea>> Get(
        Guid eventId,
        Guid areaId,
        CancellationToken cancellationToken)
    {
        var area =
            await _service.GetByIdAsync(
                eventId,
                areaId,
                cancellationToken);

        if (area is null)
        {
            return NotFound();
        }

        return Ok(area);
    }

    [HttpPost]
    public async Task<ActionResult<EventArea>> Create(
        Guid eventId,
        [FromBody] CreateEventAreaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created =
                await _service.CreateAsync(
                    eventId,
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    eventId,
                    areaId = created.Id
                },
                created);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpPut("{areaId:guid}")]
    public async Task<ActionResult<EventArea>> Update(
        Guid eventId,
        Guid areaId,
        [FromBody] UpdateEventAreaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated =
                await _service.UpdateAsync(
                    eventId,
                    areaId,
                    request,
                    cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpDelete("{areaId:guid}")]
    public async Task<IActionResult> Delete(
        Guid eventId,
        Guid areaId,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted =
                await _service.DeleteAsync(
                    eventId,
                    areaId,
                    cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }
}