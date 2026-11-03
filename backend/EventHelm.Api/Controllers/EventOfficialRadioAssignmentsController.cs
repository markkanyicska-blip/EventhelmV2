using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId}/official-radio-assignments")]
public sealed class EventOfficialRadioAssignmentsController : ControllerBase
{
    private readonly IRadioAssignmentService _service;
    public EventOfficialRadioAssignmentsController(IRadioAssignmentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RadioAssignmentView>>> Get(string eventId, CancellationToken ct) => Ok(await _service.GetAssignmentsAsync(eventId, ct));

    [HttpPost]
    public async Task<ActionResult<EventOfficialRadioAssignment>> Create(string eventId, SaveRadioAssignmentRequest request, CancellationToken ct)
    {
        try { var item = await _service.CreateAssignmentAsync(eventId, request, ct); return Created($"/api/events/{eventId}/official-radio-assignments/{item.Id}", item); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{assignmentId}")]
    public async Task<ActionResult<EventOfficialRadioAssignment>> Update(string eventId, string assignmentId, SaveRadioAssignmentRequest request, CancellationToken ct)
    {
        try { var item = await _service.UpdateAssignmentAsync(eventId, assignmentId, request, ct); return item is null ? NotFound() : Ok(item); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{assignmentId}")]
    public async Task<IActionResult> Delete(string eventId, string assignmentId, CancellationToken ct) =>
        await _service.DeleteAssignmentAsync(eventId, assignmentId, ct) ? NoContent() : NotFound();
}
