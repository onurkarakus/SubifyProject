using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests.Integration;

/// <summary>
/// 12.2.3 isolation · 12.2.4 admin auth · 12.2.5 no freemium limit.
/// </summary>
public class SubscriptionIntegrationTests : IClassFixture<SubifyWebApplicationFactory>, IAsyncLifetime
{
    private readonly SubifyWebApplicationFactory _factory;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SubscriptionIntegrationTests(SubifyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
        await _factory.EnsureDatabaseAsync();
        await SeedCompleteSetupAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task User_cannot_see_other_users_subscriptions()
    {
        var a = await CreateUserAndLoginAsync("a@subify.local");
        var b = await CreateUserAndLoginAsync("b@subify.local");

        SetBearer(a.AccessToken);
        var create = await _client.PostAsJsonAsync("/api/subscriptions", NewSub("A-Netflix"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<IdDto>(Json);
        Assert.NotNull(created?.Id);

        SetBearer(b.AccessToken);
        var listRes = await _client.GetAsync("/api/subscriptions?page=1&pageSize=20");
        Assert.True(listRes.IsSuccessStatusCode, await listRes.Content.ReadAsStringAsync());
        var list = await listRes.Content.ReadFromJsonAsync<ListDto>(Json);
        Assert.NotNull(list);
        Assert.DoesNotContain(list.Data, s => s.Id == created.Id);

        var get = await _client.GetAsync($"/api/subscriptions/{created.Id}");
        Assert.True(
            get.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"expected 404/403, got {get.StatusCode}");
    }

    [Fact]
    public async Task User_cannot_access_admin_settings()
    {
        var user = await CreateUserAndLoginAsync("plain@subify.local");
        SetBearer(user.AccessToken);

        var res = await _client.GetAsync("/api/admin/settings");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Can_create_more_than_three_subscriptions_no_limit()
    {
        var user = await CreateUserAndLoginAsync("nolimit@subify.local");
        SetBearer(user.AccessToken);

        for (var i = 1; i <= 5; i++)
        {
            var res = await _client.PostAsJsonAsync(
                "/api/subscriptions",
                NewSub($"Sub-{i}", renewalOffsetDays: i + 2));
            Assert.True(
                res.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
                $"create {i} failed: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");
        }

        var listRes = await _client.GetAsync("/api/subscriptions?page=1&pageSize=50");
        Assert.True(listRes.IsSuccessStatusCode, await listRes.Content.ReadAsStringAsync());
        var list = await listRes.Content.ReadFromJsonAsync<ListDto>(Json);
        Assert.NotNull(list);
        Assert.True(list.Pagination.TotalItems >= 5);
    }

    private async Task SeedCompleteSetupAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var settings = await db.SystemSettings.SingleAsync();
        if (!settings.IsSetupComplete)
        {
            settings.UpdateInstance(allowPublicRegistration: true);
            settings.MarkSetupComplete();
            await db.SaveChangesAsync();
        }
    }

    private async Task<TokenDto> CreateUserAndLoginAsync(string email)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            if (await users.FindByEmailAsync(email) is null)
            {
                var user = new ApplicationUser { Id = Guid.CreateVersion7() };
                user.ApplyRegistrationProfile(email.Split('@')[0], email);
                user.EmailConfirmed = true;
                var created = await users.CreateAsync(user, "Password1");
                Assert.True(created.Succeeded, string.Join(",", created.Errors.Select(e => e.Code)));
                await users.AddToRoleAsync(user, AppRoles.User);
            }
        }

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password1"
        });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var login = await loginRes.Content.ReadFromJsonAsync<TokenDto>(Json);
        Assert.NotNull(login?.AccessToken);
        return login;
    }

    private void SetBearer(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static object NewSub(string name, int renewalOffsetDays = 10) => new
    {
        name,
        price = 10m + renewalOffsetDays,
        currency = "TRY",
        billingCycle = "monthly",
        sharedWithCount = 1,
        nextRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(renewalOffsetDays)).ToString("yyyy-MM-dd")
    };

    private sealed record TokenDto(string AccessToken, string RefreshToken);
    private sealed record IdDto(Guid Id);
    private sealed record ListDto(List<IdDto> Data, PaginationDto Pagination);
    private sealed record PaginationDto(int TotalItems);
}
