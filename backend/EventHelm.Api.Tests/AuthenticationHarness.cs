using EventHelm.Api.Controllers;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using EventHelm.Api.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventHelm.Api.Tests;

internal sealed class AuthenticationHarness : IDisposable
{
    internal const string Password = "synthetic-test-passphrase-only";
    private readonly IHost _host;

    public HttpClient Client { get; }
    public CountingUserService Users { get; } = new();
    public TestClock Clock { get; } = new();

    public AuthenticationHarness(string? password = Password, string environment = "Testing")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessGate:Password"] = password
            })
            .Build();

        // No default host: appsettings, environment variables, user secrets,
        // production services, static files and JSON stores are never loaded.
        _host = new HostBuilder()
            .UseEnvironment(environment)
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddSingleton<IUserService>(Users);
                    services.AddControllers().AddApplicationPart(typeof(AccessController).Assembly);
                    services.AddSharedPasswordAuthentication(context.HostingEnvironment);
                    services.AddDataProtection().UseEphemeralDataProtectionProvider();
                    services.PostConfigure<CookieAuthenticationOptions>(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        options => options.TimeProvider = Clock);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapGet("/test/admin-only", () => "not reachable")
                            .RequireAuthorization(policy => policy.RequireRole("SystemAdministrator"));
                    });
                }), options => options.SuppressEnvironmentConfiguration = true)
            .Start();

        Client = _host.GetTestClient();
        Client.BaseAddress = new Uri("https://localhost");
    }

    public void Dispose()
    {
        Client.Dispose();
        _host.Dispose();
    }
}

internal sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan duration) => _now += duration;
}

internal sealed class CountingUserService : IUserService
{
    public int Reads { get; private set; }

    public Task<IReadOnlyList<User>> GetAllAsync()
    {
        Reads++;
        return Task.FromResult<IReadOnlyList<User>>([]);
    }

    public Task<User?> GetByIdAsync(Guid id) => throw new NotSupportedException();
    public Task<User> CreateAsync(User user) => throw new NotSupportedException();
    public Task<User?> UpdateAsync(Guid id, User user) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(Guid id) => throw new NotSupportedException();
}
