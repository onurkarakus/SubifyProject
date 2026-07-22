using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Registers concrete <see cref="IDataSeeder"/> types from the Infrastructure assembly.
/// Adding a new seeder class that implements <see cref="IDataSeeder"/> is enough — no manual DI line.
/// </summary>
public static class DataSeederRegistration
{
    public static IServiceCollection AddDataSeeders(this IServiceCollection services)
    {
        var seederInterface = typeof(IDataSeeder);
        var assembly = typeof(DataSeederRegistration).Assembly;

        var seederTypes = assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false, IsPublic: true }
                && seederInterface.IsAssignableFrom(t));

        foreach (var type in seederTypes)
        {
            services.AddScoped(seederInterface, type);
        }

        return services;
    }
}
