using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/official-assignments")]
public sealed class EventOfficialAssignmentsController
    : ControllerBase
{
    private readonly IEventOfficialAssignmentService _service;

    public EventOfficialAssignmentsController(
        IEventOfficialAssignmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<EventOfficialAssignmentDetails>>> GetAll(
        Guid eventId)
    {
        return Ok(
            await _service.GetByEventAsync(eventId));
    }

    [HttpGet("{assignmentId:guid}")]
    public async Task<ActionResult<
        EventOfficialAssignmentDetails>> GetById(
        Guid eventId,
        Guid assignmentId)
    {
        var item =
            await _service.GetByIdAsync(
                eventId,
                assignmentId);

        return item is null
            ? NotFound()
            : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<
        EventOfficialAssignmentDetails>> Create(
        Guid eventId,
        SaveEventOfficialAssignmentRequest request)
    {
        try
        {
            var created =
                await _service.CreateAsync(
                    eventId,
                    request);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    eventId,
                    assignmentId = created.Id
                },
                created);
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or InvalidOperationException)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPut("{assignmentId:guid}")]
    public async Task<ActionResult<
        EventOfficialAssignmentDetails>> Update(
        Guid eventId,
        Guid assignmentId,
        SaveEventOfficialAssignmentRequest request)
    {
        try
        {
            var updated =
                await _service.UpdateAsync(
                    eventId,
                    assignmentId,
                    request);

            return updated is null
                ? NotFound()
                : Ok(updated);
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or InvalidOperationException)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpDelete("{assignmentId:guid}")]
    public async Task<IActionResult> Delete(
        Guid eventId,
        Guid assignmentId)
    {
        var deleted =
            await _service.DeleteAsync(
                eventId,
                assignmentId);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
