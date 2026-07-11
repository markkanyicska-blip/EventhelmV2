using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create([FromBody] User user)
    {
        if (string.IsNullOrWhiteSpace(user.Name))
            return BadRequest("A név kötelező.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return BadRequest("Az e-mail cím kötelező.");

        var created = await _userService.CreateAsync(user);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<User>> Update(Guid id, [FromBody] User user)
    {
        if (string.IsNullOrWhiteSpace(user.Name))
            return BadRequest("A név kötelező.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return BadRequest("Az e-mail cím kötelező.");

        var updated = await _userService.UpdateAsync(id, user);

        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _userService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}