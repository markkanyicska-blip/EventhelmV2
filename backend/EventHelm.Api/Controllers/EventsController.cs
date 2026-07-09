using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
	private readonly IEventService _eventService;

	public EventsController(IEventService eventService)
	{
		_eventService = eventService;
	}

	[HttpGet]
	public async Task<IActionResult> GetEvents()
	{
		return Ok(await _eventService.GetAllAsync());
	}

	[HttpPost]
	public async Task<IActionResult> CreateEvent(Event eventItem)
	{
		return Ok(await _eventService.CreateAsync(eventItem));
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> Update(Guid id, Event eventItem)
	{
		var result = await _eventService.UpdateAsync(id, eventItem);

		if (result == null)
			return NotFound();

		return Ok(result);
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete(Guid id)
	{
		var deleted = await _eventService.DeleteAsync(id);

		if (!deleted)
			return NotFound();

		return NoContent();
	}
}