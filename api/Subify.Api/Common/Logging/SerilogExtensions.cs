using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Serilog;
using Serilog.Events;

namespace Subify.Api.Common.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog as the host logger (console + rolling file).
    /// Does not log request bodies (passwords/tokens stay out of request logging).
    /// </summary>
    public static WebApplicationBuilder AddSubifySerilog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Subify.Api")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .Enrich.WithMachineName()
            .CreateLogger();

        builder.Host.UseSerilog(Log.Logger, dispose: true);

        return builder;
    }

    public static IApplicationBuilder UseSubifySerilogRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = GetRequestLogLevel;
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);

                // Prefer nameidentifier; with MapInboundClaims=false JWT uses "sub"
                var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? httpContext.User?.FindFirstValue("sub");

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    diagnosticContext.Set("UserId", userId);
                }

                // Never log Authorization / Cookie headers or request body here.
            };
        });
    }

    private static LogEventLevel GetRequestLogLevel(HttpContext httpContext, double _, Exception? exception)
    {
        if (exception is not null)
        {
            return LogEventLevel.Error;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;

        // Health probes should not flood Information logs
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return LogEventLevel.Debug;
        }

        var statusCode = httpContext.Response.StatusCode;

        return statusCode switch
        {
            >= 500 => LogEventLevel.Error,
            >= 400 => LogEventLevel.Warning,
            _ => LogEventLevel.Information
        };
    }
}
