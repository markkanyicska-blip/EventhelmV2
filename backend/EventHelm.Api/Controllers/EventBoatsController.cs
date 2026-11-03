using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/boats")]
public sealed class EventBoatsController : ControllerBase
{
    private readonly EventBoatService _service;

    public EventBoatsController(EventBoatService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventBoat>>> GetAll(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var boats = await _service.GetAllAsync(
            eventId,
            cancellationToken);

        return Ok(boats);
    }

    [HttpGet("{eventBoatId:guid}")]
    public async Task<ActionResult<EventBoat>> Get(
        Guid eventId,
        Guid eventBoatId,
        CancellationToken cancellationToken)
    {
        var boat = await _service.GetByIdAsync(
            eventId,
            eventBoatId,
            cancellationToken);

        if (boat is null)
        {
            return NotFound();
        }

        return Ok(boat);
    }

    [HttpPost]
    public async Task<ActionResult<EventBoat>> Create(
        Guid eventId,
        [FromBody] CreateEventBoatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateAsync(
                eventId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    eventId,
                    eventBoatId = created.Id
                },
                created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{eventBoatId:guid}")]
    public async Task<ActionResult<EventBoat>> Update(
        Guid eventId,
        Guid eventBoatId,
        [FromBody] UpdateEventBoatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateAsync(
                eventId,
                eventBoatId,
                request,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{eventBoatId:guid}")]
    public async Task<IActionResult> Delete(
        Guid eventId,
        Guid eventBoatId,
        CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(
            eventId,
            eventBoatId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}