using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Providers;
using Subify.Application.Features.Providers.ListProviders;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.2.1 — list active providers with search.</summary>
public class ListProvidersHandlerTests
{
    [Fact]
    public async Task Lists_only_active_providers_ordered_by_name()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.SeedAsync("Netflix", "netflix", "TR", active: true);
        await harness.SeedAsync("Spotify", "spotify", "TR", active: true);
        await harness.SeedAsync("Dead", "dead", "TR", active: false);

        var result = await harness.HandleAsync(new ListProvidersQuery());
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Data.Count);
        Assert.Equal(["Netflix", "Spotify"], result.Value.Data.Select(p => p.Name).ToArray());
        Assert.DoesNotContain(result.Value.Data, p => p.Slug == "dead");
    }

    [Fact]
    public async Task Search_filters_by_name_or_slug()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.SeedAsync("Netflix", "netflix", "TR");
        await harness.SeedAsync("Disney+", "disney-plus", "TR");
        await harness.SeedAsync("Spotify", "spotify", "TR");

        var byName = await harness.HandleAsync(new ListProvidersQuery(Search: "flix"));
        Assert.Single(byName.Value.Data);
        Assert.Equal("Netflix", byName.Value.Data[0].Name);

        var bySlug = await harness.HandleAsync(new ListProvidersQuery(Search: "disney"));
        Assert.Single(bySlug.Value.Data);
        Assert.Equal("disney-plus", bySlug.Value.Data[0].Slug);
    }

    [Fact]
    public async Task Region_includes_exact_and_global()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.SeedAsync("Local TR", "local-tr", "TR");
        await harness.SeedAsync("ChatGPT", "chatgpt", "GLOBAL");
        await harness.SeedAsync("US Only", "us-only", "US");

        var tr = await harness.HandleAsync(new ListProvidersQuery(Region: "tr"));
        Assert.Equal(2, tr.Value.Data.Count);
        Assert.Contains(tr.Value.Data, p => p.Slug == "local-tr");
        Assert.Contains(tr.Value.Data, p => p.Slug == "chatgpt");
        Assert.DoesNotContain(tr.Value.Data, p => p.Slug == "us-only");
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private Harness(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<ListProvidersHandler>();

            var provider = services.BuildServiceProvider();
            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<SubifyDbContext>().Database.EnsureCreatedAsync();
            }

            return new Harness(connection, provider);
        }

        public async Task SeedAsync(string name, string slug, string region, bool active = true)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var p = Provider.CreateCatalog(name, slug, "TRY", 10m, BillingCycle.Monthly, region);
            if (!active)
            {
                p.Deactivate();
            }

            db.Providers.Add(p);
            await db.SaveChangesAsync();
        }

        public async Task<Result<ListProvidersResponse>> HandleAsync(ListProvidersQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListProvidersHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
