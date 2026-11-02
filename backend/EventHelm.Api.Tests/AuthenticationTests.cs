using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EventHelm.Api.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task Anonymous_status_is_false()
    {
        using var harness = new AuthenticationHarness();
        await AssertStatus(harness.Client, false);
    }

    [Fact]
    public async Task Login_sets_cookie_and_authenticates_subsequent_requests()
    {
        using var harness = new AuthenticationHarness();
        using var response = await Login(harness.Client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<AccessStatus>())!.Authenticated);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("EventHelm.Access=", cookie);
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.Contains("samesite=lax", cookie.ToLowerInvariant());
        UseCookie(harness.Client, cookie);

        await AssertStatus(harness.Client, true);
        using var protectedResponse = await harness.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        Assert.Equal(1, harness.Users.Reads);
    }

    [Theory]
    [InlineData("Development", "http://localhost", false)]
    [InlineData("Development", "https://localhost", true)]
    [InlineData("Production", "http://localhost", true)]
    public async Task Cookie_secure_policy_preserves_environment_behavior(
        string environment, string baseAddress, bool secure)
    {
        using var harness = new AuthenticationHarness(environment: environment);
        harness.Client.BaseAddress = new Uri(baseAddress);
        using var response = await Login(harness.Client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Equal(secure, cookie.Contains("; secure", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("incorrect")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Invalid_password_does_not_create_a_session(string password)
    {
        using var harness = new AuthenticationHarness();
        using var response = await harness.Client.PostAsJsonAsync(
            "/api/access/login", new { password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        await AssertStatus(harness.Client, false);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Missing_password_configuration_returns_unavailable(string? configuredPassword)
    {
        using var harness = new AuthenticationHarness(configuredPassword);
        using var response = await Login(harness.Client);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        await AssertStatus(harness.Client, false);
    }

    [Theory]
    [InlineData("GET", "/api/users")]
    [InlineData("POST", "/api/users")]
    [InlineData("GET", "/api/events")]
    [InlineData("GET", "/api/race-control")]
    [InlineData("POST", "/api/access/logout")]
    public async Task Anonymous_API_requests_return_401_without_redirects_or_service_access(
        string method, string path)
    {
        using var harness = new AuthenticationHarness();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await harness.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal(0, harness.Users.Reads);
    }

    [Fact]
    public async Task Logout_expires_cookie_and_browser_without_cookie_is_anonymous()
    {
        using var harness = new AuthenticationHarness();
        using var login = await Login(harness.Client);
        UseCookie(harness.Client, Assert.Single(login.Headers.GetValues("Set-Cookie")));

        using var logout = await harness.Client.PostAsync("/api/access/logout", null);

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.False((await logout.Content.ReadFromJsonAsync<AccessStatus>())!.Authenticated);
        var clearedCookie = Assert.Single(logout.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("EventHelm.Access=;", clearedCookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", clearedCookie);
        harness.Client.DefaultRequestHeaders.Remove("Cookie");
        await AssertStatus(harness.Client, false);
        using var protectedResponse = await harness.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task Expired_cookie_is_rejected_without_real_time_delays()
    {
        using var harness = new AuthenticationHarness();
        using var login = await Login(harness.Client);
        UseCookie(harness.Client, Assert.Single(login.Headers.GetValues("Set-Cookie")));
        harness.Clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromSeconds(1));

        await AssertStatus(harness.Client, false);
        using var response = await harness.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, harness.Users.Reads);
    }

    [Fact]
    public async Task Malformed_cookie_is_rejected()
    {
        using var harness = new AuthenticationHarness();
        UseCookie(harness.Client, "EventHelm.Access=synthetic-invalid-ticket");

        await AssertStatus(harness.Client, false);
        using var response = await harness.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_but_forbidden_returns_403_without_ending_session()
    {
        using var harness = new AuthenticationHarness();
        using var login = await Login(harness.Client);
        UseCookie(harness.Client, Assert.Single(login.Headers.GetValues("Set-Cookie")));

        using var response = await harness.Client.GetAsync("/test/admin-only");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(response.Headers.Location);
        await AssertStatus(harness.Client, true);
    }

    private static Task<HttpResponseMessage> Login(HttpClient client) =>
        client.PostAsJsonAsync("/api/access/login", new { password = AuthenticationHarness.Password });

    private static void UseCookie(HttpClient client, string setCookie)
    {
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", setCookie.Split(';')[0]);
    }

    private static async Task AssertStatus(HttpClient client, bool authenticated)
    {
        using var response = await client.GetAsync("/api/access/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(authenticated,
            (await response.Content.ReadFromJsonAsync<AccessStatus>())!.Authenticated);
    }

    private sealed record AccessStatus(bool Authenticated);
}
