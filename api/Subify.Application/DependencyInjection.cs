using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Behaviors;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Categories;

namespace Subify.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddScoped<ICategoryNameLookup, CategoryNameLookup>();
        services.AddScoped<IActivityLogger, ActivityLogger>();

        return services;
    }
}

