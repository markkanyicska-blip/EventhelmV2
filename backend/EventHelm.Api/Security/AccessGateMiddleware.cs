using Microsoft.AspNetCore.Http;

namespace EventHelm.Api.Security;

public sealed class AccessGateMiddleware
{
    private readonly RequestDelegate _next;

    public AccessGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Bejelentkezes szukseges."
                });
                return;
            }
        }

        await _next(context);
    }
}
