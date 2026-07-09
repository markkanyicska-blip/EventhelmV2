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
		var events = await _eventService.GetAllAsync();
		return Ok(events);
	}

	[HttpPost]
	public async Task<IActionResult> CreateEvent(Event eventItem)
	{
		var created = await _eventService.CreateAsync(eventItem);
		return Ok(created);
	}
}