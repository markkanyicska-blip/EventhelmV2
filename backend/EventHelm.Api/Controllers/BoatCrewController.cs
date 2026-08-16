using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("boats/{boatId:guid}/crew")]
public sealed class BoatCrewController : ControllerBase
{
    private readonly IBoatCrewService _service;

    public BoatCrewController(IBoatCrewService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BoatCrewAssignment>>> GetAll(
        Guid boatId)
    {
        var crew = await _service.GetByBoatIdAsync(boatId);

        return Ok(crew);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoatCrewAssignment>> Get(
        Guid boatId,
        Guid id)
    {
        var assignment = await _service.GetByIdAsync(id);

        if (assignment is null)
        {
            return NotFound();
        }

        if (assignment.BoatId != boatId)
        {
            return NotFound();
        }

        return Ok(assignment);
    }

    [HttpPost]
    public async Task<ActionResult<BoatCrewAssignment>> Create(
        Guid boatId,
        CreateBoatCrewAssignmentRequest request)
    {
        var created = await _service.CreateAsync(boatId, request);

        return CreatedAtAction(
            nameof(Get),
            new
            {
                boatId,
                id = created.Id
            },
            created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BoatCrewAssignment>> Update(
        Guid boatId,
        Guid id,
        UpdateBoatCrewAssignmentRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);

        if (updated is null)
        {
            return NotFound();
        }

        if (updated.BoatId != boatId)
        {
            return BadRequest();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid boatId,
        Guid id)
    {
        var assignment = await _service.GetByIdAsync(id);

        if (assignment is null)
        {
            return NotFound();
        }

        if (assignment.BoatId != boatId)
        {
            return BadRequest();
        }

        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}