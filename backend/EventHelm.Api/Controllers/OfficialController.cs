using EventHelm.Api.Models;
using EventHelm.Api.Services;
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
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _officialService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var official = await _officialService.GetByIdAsync(id);

        return official is null ? NotFound() : Ok(official);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Official official)
    {
        if (string.IsNullOrWhiteSpace(official.Name))
        {
            return BadRequest("A rendező neve kötelező.");
        }

        var created = await _officialService.CreateAsync(official);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Official official)
    {
        if (string.IsNullOrWhiteSpace(official.Name))
        {
            return BadRequest("A rendező neve kötelező.");
        }

        var updated = await _officialService.UpdateAsync(id, official);

        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _officialService.DeleteAsync(id);

        return deleted ? NoContent() : NotFound();
    }
}