using Microsoft.Extensions.DependencyInjection;

namespace Subify.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
           configuration.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly); 
        });

        return services;
    }
}