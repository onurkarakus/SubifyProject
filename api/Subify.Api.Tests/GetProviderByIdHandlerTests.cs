using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Providers;
using Subify.Application.Features.Providers.GetProviderById;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.2.2 — get provider by id.</summary>
public class GetProviderByIdHandlerTests
{
    [Fact]
    public async Task Get_active_provider_returns_detail()
    {
        await using var harness = await Harness.CreateAsync();
        var id = await harness.SeedAsync("Netflix", "netflix", active: true);

        var result = await harness.HandleAsync(id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("Netflix", result.Value.Name);
        Assert.Equal("netflix", result.Value.Slug);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Get_missing_returns_not_found()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.HandleAsync(Guid.CreateVersion7());
        Assert.Equal(DomainErrors.ProviderErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_inactive_or_soft_deleted_returns_not_found()
    {
        await using var harness = await Harness.CreateAsync();
        var id = await harness.SeedAsync("Dead", "dead", active: false);

        // Deactivate sets DeletedAt → global filter → NotFound
        var result = await harness.HandleAsync(id);
        Assert.Equal(DomainErrors.ProviderErrors.NotFound.Code, result.Error.Code);
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
            services.AddScoped<GetProviderByIdHandler>();

            var provider = services.BuildServiceProvider();
            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<SubifyDbContext>().Database.EnsureCreatedAsync();
            }

            return new Harness(connection, provider);
        }

        public async Task<Guid> SeedAsync(string name, string slug, bool active)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var p = Provider.CreateCatalog(name, slug, "TRY", 99m, BillingCycle.Monthly, "TR");
            if (!active)
            {
                p.Deactivate();
            }

            db.Providers.Add(p);
            await db.SaveChangesAsync();
            return p.Id;
        }

        public async Task<Result<ProviderResponse>> HandleAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetProviderByIdHandler>()
                .Handle(new GetProviderByIdQuery(id), CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
