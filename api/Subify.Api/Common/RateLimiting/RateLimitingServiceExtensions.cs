using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Subify.Domain.Errors;

namespace Subify.Api.Common.RateLimiting;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddSubifyRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                var error = DomainErrors.SystemErrors.TooManyRequests;
                var httpContext = context.HttpContext;

                var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? (double?)retryAfter.TotalSeconds
                    : null;

                if (retryAfterSeconds is not null)
                {
                    httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfterSeconds.Value)).ToString();
                }

                var extensions = new Dictionary<string, object?>
                {
                    ["errorCode"] = error.Code,
                    ["traceId"] = httpContext.TraceIdentifier
                };

                if (retryAfterSeconds is not null)
                {
                    extensions["retryAfter"] = (int)Math.Ceiling(retryAfterSeconds.Value);
                }

                await Results.Problem(
                    detail: error.Description,
                    instance: httpContext.Request.Path,
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: error.Title,
                    type: $"https://api.subify.app/errors/{error.Code}",
                    extensions: extensions).ExecuteAsync(httpContext);
            };

            limiterOptions.AddPolicy(RateLimitingOptions.LoginPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => CreateWindowOptions(options.Login)));

            limiterOptions.AddPolicy(RateLimitingOptions.RegisterPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => CreateWindowOptions(options.Register)));

            // Partition by authenticated user when present; otherwise by IP (for pre-auth misuse)
            limiterOptions.AddPolicy(RateLimitingOptions.AiPolicy, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? httpContext.User?.FindFirst("sub")?.Value
                    ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var key = !string.IsNullOrWhiteSpace(userId)
                    ? $"user:{userId}"
                    : $"ip:{GetClientKey(httpContext)}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => CreateWindowOptions(options.Ai));
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSubifyRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    private static FixedWindowRateLimiterOptions CreateWindowOptions(RateLimitWindowOptions window)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, window.PermitLimit),
            Window = TimeSpan.FromSeconds(Math.Max(1, window.WindowSeconds)),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }

    private static string GetClientKey(HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
