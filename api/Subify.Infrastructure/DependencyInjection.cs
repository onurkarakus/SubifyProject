using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Domain.Entities;
using Subify.Application.Features.Ai.AnalyzeSubscriptions;
using Subify.Infrastructure.Ai;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Authorization;
using Subify.Infrastructure.Background;
using Subify.Infrastructure.Email;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Identity;
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

        // Task 3.4 — password, lockout, unique email (see IdentitySecurityDefaults)
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(IdentityOptionsConfiguration.Apply)
        .AddEntityFrameworkStores<SubifyDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IExchangeRateLookup, ExchangeRateLookup>();

        // 8.4 — background host (BackgroundService, not Hangfire)
        services.Configure<BackgroundJobsOptions>(configuration.GetSection(BackgroundJobsOptions.SectionName));

        // 6.2 — FX provider, snapshot sync, periodic job, GET cache
        services.Configure<ExchangeRateOptions>(configuration.GetSection(ExchangeRateOptions.SectionName));
        // Env override: EXCHANGE_RATE_API_KEY → ExchangeRates:ApiKey
        var envKey = configuration["EXCHANGE_RATE_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("EXCHANGE_RATE_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            services.PostConfigure<ExchangeRateOptions>(o => o.ApiKey = envKey);
        }

        // Optional: BACKGROUND_FX_INTERVAL → ExchangeRates:SyncInterval (8.4.2)
        var fxInterval = configuration["BACKGROUND_FX_INTERVAL"]
                         ?? Environment.GetEnvironmentVariable("BACKGROUND_FX_INTERVAL");
        if (!string.IsNullOrWhiteSpace(fxInterval))
        {
            services.PostConfigure<ExchangeRateOptions>(o => o.SyncInterval = fxInterval);
        }

        services.AddMemoryCache();
        services.AddHttpClient(HttpExchangeRateClient.HttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExchangeRateOptions>>().Value;
            client.Timeout = opts.HttpTimeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubifyOS/1.0 (+exchange-rates)");
        });
        services.AddScoped<IExchangeRateClient, HttpExchangeRateClient>();
        services.AddScoped<IExchangeRateSyncService, ExchangeRateSyncService>();
        services.AddHostedService<ExchangeRateSyncBackgroundService>();

        // 9.x — BYOK OpenAI-compatible AI
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<AiAnalyzeOptions>(configuration.GetSection(AiAnalyzeOptions.SectionName));
        services.AddHttpClient(OpenAiCompatibleClient.HttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.HttpTimeoutSeconds, 10, 180));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubifyOS/1.0 (+ai)");
        });
        services.AddScoped<IAiClient, OpenAiCompatibleClient>();
        services.AddScoped<IAiSettingsResolver, AiSettingsResolver>();

        // 15.x — EmailSend (SMTP from SystemSettings; noop when not configured)
        services.Configure<EmailJobsOptions>(configuration.GetSection(EmailJobsOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
        services.AddScoped<IRenewalReminderService, RenewalReminderService>();
        services.AddHostedService<RenewalReminderBackgroundService>();

        // Same scoped DbContext instance for both abstractions (task 2.4.1 / 2.4.2)
        services.AddScoped<ISubifyDbContext>(provider => provider.GetRequiredService<SubifyDbContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SubifyDbContext>());

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? new JwtOptions();

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
            // Task 3.1.5: ClockSkew from JwtOptions (default 30s)
            options.TokenValidationParameters = JwtTokenValidation.CreateParameters(jwtOptions);
        });

        services.AddAuthorization(AuthPolicies.Configure);

        return services;
    }
}