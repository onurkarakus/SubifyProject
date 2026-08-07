using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Common.Setup;

/// <summary>
/// While setup is incomplete, only setup/auth/health/docs paths are open (3S.1.4).
/// After SuperAdmin exists they may login; business APIs still gated until complete.
/// </summary>
public sealed class SetupGateMiddleware
{
    private static readonly PathString[] AlwaysAllowedPrefixes =
    [
        new("/health"),
        new("/api/setup"),
        new("/api/auth/login"),
        new("/api/auth/register"),
        new("/api/auth/accept-invite"),
        new("/api/auth/forgot-password"),
        new("/api/auth/reset-password"),
        new("/api/auth/refresh"),
        new("/api/auth/refresh-token"),
        new("/api/auth/logout"),
        // 3S.6.3 — optional AI ping during wizard (still SuperAdmin-authorized on endpoint)
        new("/api/admin/settings/test-ai"),
        new("/openapi"),
        new("/scalar")
    ];

    private readonly RequestDelegate _next;

    public SetupGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SubifyDbContext db)
    {
        var path = context.Request.Path;

        if (IsAlwaysAllowed(path) || HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var setupComplete = await db.SystemSettings
            .AsNoTracking()
            .Select(s => (bool?)s.IsSetupComplete)
            .FirstOrDefaultAsync(context.RequestAborted);

        if (setupComplete is true or null)
        {
            // null = no row yet (allow; seed should create). true = open app.
            await _next(context);
            return;
        }

        // Setup incomplete: only allowlisted paths (above). Everything else → 403.
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://api.subify.app/errors/AUTH_017",
            title = "Setup Required",
            status = 403,
            detail = "First-run setup is not complete. Use /api/setup/* endpoints, then POST /api/setup/complete.",
            errorCode = "AUTH_017",
            instance = path.Value
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static bool IsAlwaysAllowed(PathString path)
    {
        if (path == "/" || path.Value is null or "")
        {
            return true;
        }

        foreach (var prefix in AlwaysAllowedPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
