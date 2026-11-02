using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace EventHelm.Api.Controllers;

[ApiController]
[Route("api/access")]
public sealed class AccessController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AccessController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public sealed record LoginRequest(string Password);

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var expected = _configuration["AccessGate:Password"];

        if (string.IsNullOrWhiteSpace(expected))
        {
            return StatusCode(503, new { error = "A belepesi jelszo nincs beallitva." });
        }

        var suppliedBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(request.Password ?? ""));

        var expectedBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(expected));

        if (!CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes))
        {
            return Unauthorized(new { error = "Hibas jelszo." });
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "EventHelmUser") },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Ok(new { authenticated = true });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new { authenticated = false });
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            authenticated = User.Identity?.IsAuthenticated == true
        });
    }
}
