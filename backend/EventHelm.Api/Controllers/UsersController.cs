using EventHelm.Api.Models;
using EventHelm.Api.Services;
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
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _userService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);

        return user is null
            ? NotFound()
            : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Name))
        {
            return BadRequest("A név kötelező.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest("Az e-mail-cím kötelező.");
        }

        var created = await _userService.CreateAsync(user);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, User user)
    {
        if (string.IsNullOrWhiteSpace(user.Name))
        {
            return BadRequest("A név kötelező.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest("Az e-mail-cím kötelező.");
        }

        var updated = await _userService.UpdateAsync(id, user);

        return updated is null
            ? NotFound()
            : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _userService.DeleteAsync(id);

        return deleted
            ? NoContent()
            : NotFound();
    }
}