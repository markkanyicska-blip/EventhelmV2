using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoatsController : ControllerBase
{
    private readonly IBoatService _boatService;

    public BoatsController(IBoatService boatService)
    {
        _boatService = boatService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Boat>>> GetAll()
    {
        var boats = await _boatService.GetAllAsync();
        return Ok(boats);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Boat>> GetById(Guid id)
    {
        var boat = await _boatService.GetByIdAsync(id);

        if (boat is null)
        {
            return NotFound();
        }

        return Ok(boat);
    }

    [HttpPost]
    public async Task<ActionResult<Boat>> Create([FromBody] Boat boat)
    {
        var created = await _boatService.CreateAsync(boat);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Boat>> Update(Guid id, [FromBody] Boat boat)
    {
        var updated = await _boatService.UpdateAsync(id, boat);

        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _boatService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}