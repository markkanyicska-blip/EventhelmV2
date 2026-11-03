using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/tasks")]
public sealed class EventTasksController : ControllerBase
{
    private readonly EventTaskService _service;

    public EventTasksController(
        EventTaskService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventTask>>> GetAll(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var tasks =
            await _service.GetAllAsync(
                eventId,
                cancellationToken);

        return Ok(tasks);
    }

    [HttpGet("{taskId:guid}")]
    public async Task<ActionResult<EventTask>> Get(
        Guid eventId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task =
            await _service.GetByIdAsync(
                eventId,
                taskId,
                cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<EventTask>> Create(
        Guid eventId,
        [FromBody] CreateEventTaskRequest request,
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
                    taskId = created.Id
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

    [HttpPut("{taskId:guid}")]
    public async Task<ActionResult<EventTask>> Update(
        Guid eventId,
        Guid taskId,
        [FromBody] UpdateEventTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated =
                await _service.UpdateAsync(
                    eventId,
                    taskId,
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

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete(
        Guid eventId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var deleted =
            await _service.DeleteAsync(
                eventId,
                taskId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}