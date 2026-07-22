using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Subify.Infrastructure.Persistence;

/// <summary>
/// Applies EF Core migrations at API startup with retry while PostgreSQL becomes ready.
/// Task 2.3.2 — self-host / Docker: no manual <c>dotnet ef database update</c> required.
/// </summary>
public static class DatabaseMigrator
{
    private const int DefaultMaxAttempts = 15;
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(2);

    public static async Task MigrateAsync(
        IServiceProvider services,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? delayBetweenAttempts = null,
        CancellationToken cancellationToken = default)
    {
        var delay = delayBetweenAttempts ?? DefaultDelay;

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Subify.Infrastructure.DatabaseMigrator");
        var dbContext = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Applying database migrations (attempt {Attempt}/{MaxAttempts})...",
                    attempt,
                    maxAttempts);

                var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                var pendingList = pending.ToList();

                if (pendingList.Count == 0)
                {
                    logger.LogInformation("Database is up to date; no pending migrations.");
                }
                else
                {
                    logger.LogInformation(
                        "Pending migrations: {Migrations}",
                        string.Join(", ", pendingList));

                    await dbContext.Database.MigrateAsync(cancellationToken);

                    logger.LogInformation("Database migrations applied successfully.");
                }

                return;
            }
            catch (Exception ex) when (IsTransientDatabaseError(ex) && attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Database not ready (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s...",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool IsTransientDatabaseError(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            if (current is NpgsqlException or TimeoutException or IOException)
            {
                return true;
            }

            // Socket / connection refused while Postgres container starts
            if (current is InvalidOperationException
                && current.Message.Contains("database", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
