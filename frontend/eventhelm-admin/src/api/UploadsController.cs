using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public sealed class UploadsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/jpeg",
            "image/png"
        };

    private readonly IWebHostEnvironment _environment;

    public UploadsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("official-document")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<FileUploadResponse>> UploadOfficialDocument(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Nincs feltöltendő fájl.");

        if (file.Length > MaxFileSize)
            return BadRequest("A fájl mérete legfeljebb 10 MB lehet.");

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.Contains(extension))
        {
            return BadRequest(
                "Csak PDF, JPG, JPEG vagy PNG fájl tölthető fel.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest("A fájl típusa nem támogatott.");
        }

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");
        }

        var uploadDirectory = Path.Combine(
            webRootPath,
            "uploads",
            "officials");

        Directory.CreateDirectory(uploadDirectory);

        var generatedFileName =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var fullPath = Path.Combine(
            uploadDirectory,
            generatedFileName);

        await using var stream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await file.CopyToAsync(stream, cancellationToken);

        var relativeUrl =
            $"/uploads/officials/{generatedFileName}";

        return Ok(new FileUploadResponse
        {
            Url = relativeUrl,
            OriginalFileName = Path.GetFileName(file.FileName)
        });
    }

    [HttpDelete("official-document")]
    public IActionResult DeleteOfficialDocument(
        [FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("A dokumentum URL-je kötelező.");

        const string expectedPrefix = "/uploads/officials/";

        if (!url.StartsWith(
                expectedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Érvénytelen dokumentum URL.");
        }

        var fileName = Path.GetFileName(url);

        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("Érvénytelen fájlnév.");

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");
        }

        var fullPath = Path.Combine(
            webRootPath,
            "uploads",
            "officials",
            fileName);

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        System.IO.File.Delete(fullPath);

        return NoContent();
    }
}

public sealed class FileUploadResponse
{
    public string Url { get; set; } = "";

    public string OriginalFileName { get; set; } = "";
}