using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests.Integration;

/// <summary>12.2.2 — HTTP auth flow: setup admin → complete → login → refresh → logout.</summary>
public class AuthFlowIntegrationTests : IClassFixture<SubifyWebApplicationFactory>, IAsyncLifetime
{
    private readonly SubifyWebApplicationFactory _factory;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthFlowIntegrationTests(SubifyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
        await _factory.EnsureDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Setup_admin_login_refresh_logout()
    {
        // Create first SuperAdmin via setup
        var create = await _client.PostAsJsonAsync("/api/setup/admin", new
        {
            fullName = "Owner",
            email = "owner@subify.local",
            password = "Password1"
        });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var created = await create.Content.ReadFromJsonAsync<SetupAdminDto>(Json);
        Assert.False(string.IsNullOrWhiteSpace(created?.AccessToken));

        // Finish setup so app APIs open
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var settings = await db.SystemSettings.SingleAsync();
            settings.UpdateInstance(
                instanceName: "Test",
                defaultLocale: "tr",
                defaultCurrency: "TRY",
                allowPublicRegistration: true);
            settings.MarkSetupComplete();
            await db.SaveChangesAsync();
        }

        // Login
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "owner@subify.local",
            password = "Password1"
        });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var login = await loginRes.Content.ReadFromJsonAsync<TokenDto>(Json);
        Assert.NotNull(login?.AccessToken);
        Assert.NotNull(login.RefreshToken);

        // Authenticated call
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var profile = await _client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        // Refresh
        var refreshRes = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = login.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);
        var refreshed = await refreshRes.Content.ReadFromJsonAsync<TokenDto>(Json);
        Assert.NotNull(refreshed?.AccessToken);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);

        // Logout
        var logout = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = refreshed.RefreshToken,
            allSessions = false
        });
        Assert.True(logout.IsSuccessStatusCode);

        // Old refresh should fail
        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = refreshed.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    private sealed record SetupAdminDto(string AccessToken, string RefreshToken, string Role);
    private sealed record TokenDto(string AccessToken, string RefreshToken);
}
