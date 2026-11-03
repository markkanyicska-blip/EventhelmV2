using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/boats/{boatId:guid}/defects")]
public class BoatDefectsController : ControllerBase
{
    private readonly BoatDefectService _service;
    public BoatDefectsController(BoatDefectService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BoatDefect>>> List(Guid boatId)
        => Ok(await _service.GetByBoatAsync(boatId));

    [HttpPost]
    public async Task<ActionResult<BoatDefect>> Create(Guid boatId, [FromBody] CreateDefectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReportedBy) || request.ReportedBy.Length > 150 ||
            string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 4000 ||
            !new[] { "Low", "Normal", "High", "Critical" }.Contains(request.Priority))
            return BadRequest("A bejelentő, a leírás vagy a sürgősség érvénytelen.");
        var defect = await _service.CreateAsync(boatId, new BoatDefect
        {
            ReportedBy = request.ReportedBy.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority
        });
        return defect is null ? NotFound("A motorcsónak nem található.") : StatusCode(201, defect);
    }

    [HttpPatch("{defectId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid boatId, Guid defectId, [FromBody] UpdateDefectStatusRequest request)
    {
        if (!new[] { "Reported", "InProgress", "Resolved" }.Contains(request.Status))
            return BadRequest("Érvénytelen állapot.");
        return await _service.UpdateStatusAsync(boatId, defectId, request.Status) ? NoContent() : NotFound();
    }
}

public record CreateDefectRequest(string ReportedBy, string Description, string Priority = "Normal");
public record UpdateDefectStatusRequest(string Status);
