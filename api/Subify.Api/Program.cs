using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Serilog;
using Subify.Api.Common.Cors;
using Subify.Api.Common.Exceptions;
using Subify.Api.Common.Extensions;
using Subify.Api.Common.Health;
using Subify.Api.Common.Logging;
using Subify.Api.Common.OpenApi;
using Subify.Api.Common.RateLimiting;
using Subify.Api.Common.Security;
using Subify.Api.Common.Setup;
using Subify.Application;
using Subify.Infrastructure;
using Subify.Infrastructure.Persistence.Seeding;

namespace Subify.Api;

/// <summary>Entry point. <c>partial</c> enables <c>WebApplicationFactory&lt;Program&gt;</c> (Faz 12.2).</summary>
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddSubifySerilog();

        try
        {
            Log.Information("Starting Subify OS API");

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

            // Enums as camelCase strings in JSON (billingCycle: "monthly" not 1)
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            builder.Services.AddSubifyCors(builder.Configuration, builder.Environment);
            builder.Services.AddSubifyRateLimiting(builder.Configuration);
            builder.Services.AddSubifyHealthChecks();

            builder.Services.AddProblemDetails();
            // Order matters: more specific handlers first, GlobalExceptionHandler last (always handles)
            builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddOpenApi(options =>
            {
                // 14.2.1 title/version + 3.3.4 JWT scheme
                options.AddDocumentTransformer<OpenApiInfoTransformer>();
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            var app = builder.Build();

            // Task 2.3.2 + 2.3.3: migrate then seed (skip in automated WebApplicationFactory "Testing")
            if (!app.Environment.IsEnvironment("Testing"))
            {
                await DatabaseInitializer.InitializeAsync(app.Services);
            }

            // Exception handlers (Validation → 400, unhandled → SYS_001 / 500)
            app.UseExceptionHandler();

            // 14.1.4 — baseline security headers (HSTS/CSP-for-HTML at reverse proxy)
            app.UseSubifySecurityHeaders();

            // Structured HTTP request logging (method/path/status/elapsed only — no body/secrets)
            app.UseSubifySerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                // FallbackPolicy requires auth; docs stay public (task 3.3.4)
                app.MapOpenApi().AllowAnonymous();
                app.MapScalarApiReference(options =>
                {
                    options
                        .WithTitle("Subify OS API")
                        .WithTheme(ScalarTheme.Purple)
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                        // Prefer HTTP Bearer so Scalar shows a token field (not OAuth client flow)
                        .AddPreferredSecuritySchemes("Bearer");
                }).AllowAnonymous();

                // Convenience: open API docs at root in development
                app.MapGet("/", () => Results.Redirect("/scalar/v1"))
                    .ExcludeFromDescription()
                    .AllowAnonymous();
            }

            // Avoid forcing HTTPS redirect when running the local http profile
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // CORS before auth so preflight OPTIONS is handled
            app.UseSubifyCors();

            app.UseSubifyRateLimiting();

            app.UseAuthentication();
            app.UseAuthorization();

            // 3S.1.4 — while setup incomplete, block app APIs (allow setup/auth/health/docs)
            app.UseMiddleware<SetupGateMiddleware>();

            app.MapEndpoints();
            app.MapSubifyReadyHealthCheck();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Subify OS API terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

