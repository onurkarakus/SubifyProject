using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Persistence;
using Subify.Infrastructure.Persistence.Seeding;

namespace Subify.Infrastructure;

public static class DependencyInjection
{
public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)    
{
        services.AddDbContext<SubifyDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Task 2.3.3: IDataSeeder implementations (auto-discovered in this assembly)
        services.AddDataSeeders();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<SubifyDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Same scoped DbContext instance for both abstractions (task 2.4.1 / 2.4.2)
        services.AddScoped<ISubifyDbContext>(provider => provider.GetRequiredService<SubifyDbContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SubifyDbContext>());

        var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            // Keep JWT claim names as issued (sub, email) so CurrentUserService can resolve them consistently
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = jwtOptions?.Audience,
                ValidIssuer = jwtOptions?.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions?.SecretKey ?? string.Empty)),
                NameClaimType = AppClaimTypes.Subject,
                RoleClaimType = AppClaimTypes.Role
            };
        });

        services.AddAuthorization();

        return services;
    }
}