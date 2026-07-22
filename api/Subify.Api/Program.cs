using System.Reflection;
using Scalar.AspNetCore;
using Serilog;
using Subify.Api.Common.Cors;
using Subify.Api.Common.Exceptions;
using Subify.Api.Common.Extensions;
using Subify.Api.Common.Health;
using Subify.Api.Common.Logging;
using Subify.Api.Common.OpenApi;
using Subify.Api.Common.RateLimiting;
using Subify.Application;
using Subify.Infrastructure;
using Subify.Infrastructure.Persistence.Seeding;

namespace Subify.Api;

public class Program
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

            builder.Services.AddSubifyCors(builder.Configuration, builder.Environment);
            builder.Services.AddSubifyRateLimiting(builder.Configuration);
            builder.Services.AddSubifyHealthChecks();

            builder.Services.AddProblemDetails();
            // Order matters: more specific handlers first, GlobalExceptionHandler last (always handles)
            builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            var app = builder.Build();

            // Task 2.3.2 + 2.3.3: migrate then run idempotent seeders before accepting traffic
            await DatabaseInitializer.InitializeAsync(app.Services);

            // Exception handlers (Validation → 400, unhandled → SYS_001 / 500)
            app.UseExceptionHandler();

            // Structured HTTP request logging (method/path/status/elapsed only — no body/secrets)
            app.UseSubifySerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options
                        .WithTitle("Subify OS API")
                        .WithTheme(ScalarTheme.Purple)
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                        // Prefer HTTP Bearer so Scalar shows a token field (not OAuth client flow)
                        .AddPreferredSecuritySchemes("Bearer");
                });

                // Convenience: open API docs at root in development
                app.MapGet("/", () => Results.Redirect("/scalar/v1"))
                    .ExcludeFromDescription();
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

