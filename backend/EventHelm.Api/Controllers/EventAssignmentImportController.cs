using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/assignment-import")]
public sealed class EventAssignmentImportController : ControllerBase
{
    private const long MaximumFileSize = 15_000_000;

    private readonly IEventAssignmentImportService _service;

    public EventAssignmentImportController(
        IEventAssignmentImportService service)
    {
        _service = service;
    }

    /// <summary>
    /// Beolvassa az Excel-fájlt, ellenőrzi a sorokat,
    /// de még nem módosítja az adatbázist.
    /// </summary>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumFileSize)]
    [ProducesResponseType<EventAssignmentImportPreview>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventAssignmentImportPreview>> Preview(
        Guid eventId,
        [FromForm] IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A feltöltött fájl üres.");
        }

        if (file.Length > MaximumFileSize)
        {
            return BadRequest("A fájl legfeljebb 15 MB lehet.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "Jelenleg csak .xlsx fájl importálható.");
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var preview = await _service.PreviewAsync(
                eventId,
                stream,
                file.FileName);

            return Ok(preview);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// Az előnézetben jóváhagyott sorokat beírja az adatbázisba.
    /// </summary>
    [HttpPost("commit")]
    [Consumes("application/json")]
    [ProducesResponseType<EventAssignmentImportResult>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventAssignmentImportResult>> Commit(
        Guid eventId,
        [FromBody] EventAssignmentImportCommitRequest? request)
    {
        if (request?.Rows is null || request.Rows.Count == 0)
        {
            return BadRequest("Nincs importálható sor.");
        }

        try
        {
            var result = await _service.CommitAsync(
                eventId,
                request);

            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}