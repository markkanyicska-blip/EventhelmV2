using EventHelm.Api.Models;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/radios")]
public sealed class RadioEquipmentController : ControllerBase
{
    private readonly IRadioAssignmentService _service;
    public RadioEquipmentController(IRadioAssignmentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RadioEquipment>>> Get(CancellationToken ct) => Ok(await _service.GetRadiosAsync(ct));

    [HttpPost]
    public async Task<ActionResult<RadioEquipment>> Create(SaveRadioRequest request, CancellationToken ct)
    {
        try { var radio = await _service.CreateRadioAsync(request, ct); return Created($"/api/radios/{radio.Id}", radio); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RadioEquipment>> Update(string id, SaveRadioRequest request, CancellationToken ct)
    {
        try { var radio = await _service.UpdateRadioAsync(id, request, ct); return radio is null ? NotFound() : Ok(radio); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
    }
}
