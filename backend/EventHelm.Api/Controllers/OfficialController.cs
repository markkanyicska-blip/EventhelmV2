using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OfficialsController : ControllerBase
{
    private readonly IOfficialService _officialService;

    public OfficialsController(IOfficialService officialService)
    {
        _officialService = officialService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Official>>> GetAll()
    {
        var officials = await _officialService.GetAllAsync();
        return Ok(officials);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Official>> GetById(Guid id)
    {
        var official = await _officialService.GetByIdAsync(id);

        if (official is null)
            return NotFound();

        return Ok(official);
    }

    [HttpPost]
    public async Task<ActionResult<Official>> Create([FromBody] Official official)
    {
        if (string.IsNullOrWhiteSpace(official.Name))
            return BadRequest("A rendező neve kötelező.");

        var created = await _officialService.CreateAsync(official);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Official>> Update(Guid id, [FromBody] Official official)
    {
        if (string.IsNullOrWhiteSpace(official.Name))
            return BadRequest("A rendező neve kötelező.");

        var updated = await _officialService.UpdateAsync(id, official);

        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _officialService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}