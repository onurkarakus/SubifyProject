using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Runs all registered <see cref="IDataSeeder"/> implementations after migrations (task 2.3.3).
/// Each seeder is responsible for its own idempotency (task 2.3.10).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Subify.Infrastructure.DatabaseSeeder");

        var seeders = scope.ServiceProvider
            .GetServices<IDataSeeder>()
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();

        if (seeders.Count == 0)
        {
            logger.LogInformation("No data seeders registered; skipping seed pipeline.");
            return;
        }

        logger.LogInformation(
            "Running {Count} data seeder(s): {Names}",
            seeders.Count,
            string.Join(", ", seeders.Select(s => $"{s.Name}#{s.Order}")));

        foreach (var seeder in seeders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation(
                "Seeder starting: {Seeder} (order {Order})",
                seeder.Name,
                seeder.Order);

            await seeder.SeedAsync(cancellationToken);

            logger.LogInformation("Seeder completed: {Seeder}", seeder.Name);
        }

        logger.LogInformation("Data seed pipeline finished.");
    }
}
