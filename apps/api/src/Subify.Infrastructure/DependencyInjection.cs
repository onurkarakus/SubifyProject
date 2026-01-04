using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Models.Entities.Users;
using Subify.Infrastructure.Persistence;
using Subify.Infrastructure.Services;

namespace Subify.Infrastructure;

public static  class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<SubifyDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Subify.Infrastructure")));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
        })
           .AddEntityFrameworkStores<SubifyDbContext>()
           .AddDefaultTokenProviders();

        

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
