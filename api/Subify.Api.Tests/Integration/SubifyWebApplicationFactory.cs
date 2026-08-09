using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests.Integration;

/// <summary>
/// WebApplicationFactory host for Faz 12.2 integration tests.
/// Replaces Postgres with shared SQLite in-memory; disables hosted jobs.
/// </summary>
public sealed class SubifyWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;
    private bool _databaseReady;

    public async Task InitializeAsync()
    {
        // Named shared-cache memory DB so multiple DbContext instances share schema
        _connection = new SqliteConnection(
            $"Data Source=subify-it-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var remove = services
                .Where(d =>
                    d.ServiceType == typeof(SubifyDbContext)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(DbContextOptions<SubifyDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                .ToList();

            foreach (var d in remove)
            {
                services.Remove(d);
            }

            services.RemoveAll<IHostedService>();

            var connection = _connection
                ?? throw new InvalidOperationException("Call InitializeAsync before creating the client.");

            services.AddDbContext<SubifyDbContext>(options => options.UseSqlite(connection));

            services.AddScoped<Subify.Application.Common.Interfaces.ISubifyDbContext>(sp =>
                sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<Subify.Application.Common.Interfaces.IUnitOfWork>(sp =>
                sp.GetRequiredService<SubifyDbContext>());
        });
    }

    public async Task EnsureDatabaseAsync()
    {
        _ = CreateClient();

        if (_databaseReady)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();

        // EnsureCreated is not idempotent on some providers when tables exist — guard
        var created = await db.Database.EnsureCreatedAsync();
        _ = created;

        var roles = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>>();
        foreach (var name in Subify.Domain.Constants.AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(name))
            {
                await roles.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole<Guid>(name)
                {
                    Id = Guid.CreateVersion7()
                });
            }
        }

        if (!await db.SystemSettings.AnyAsync())
        {
            db.SystemSettings.Add(Subify.Domain.Entities.SystemSettings.CreateDefault());
            await db.SaveChangesAsync();
        }

        _databaseReady = true;
    }
}
