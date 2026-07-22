namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Startup database bootstrap: migrate (2.3.2) then seed (2.3.3).
/// Call once before accepting HTTP traffic.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await DatabaseMigrator.MigrateAsync(services, cancellationToken: cancellationToken);
        await DatabaseSeeder.SeedAsync(services, cancellationToken);
    }
}
