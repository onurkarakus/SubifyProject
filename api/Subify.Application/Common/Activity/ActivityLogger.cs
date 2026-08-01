using Microsoft.AspNetCore.Http;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;

namespace Subify.Application.Common.Activity;

/// <summary>Default <see cref="IActivityLogger"/> (5.4.1).</summary>
public sealed class ActivityLogger : IActivityLogger
{
    private readonly ISubifyDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogger(ISubifyDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        Guid userId,
        string entityType,
        string action,
        string description,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default)
    {
        await _db.ActivityLogs.AddAsync(
            ActivityLog.Create(
                userId: userId,
                entityType: entityType,
                action: action,
                description: description,
                entityId: entityId,
                oldValues: oldValues,
                newValues: newValues,
                ipAddress: ResolveClientIp(),
                userAgent: ResolveUserAgent()),
            cancellationToken);
    }

    public async Task LogAndSaveAsync(
        Guid userId,
        string entityType,
        string action,
        string description,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(
            userId,
            entityType,
            action,
            description,
            entityId,
            oldValues,
            newValues,
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private string? ResolveClientIp()
    {
        var ctx = _httpContextAccessor.HttpContext;
        var forwarded = ctx?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        return ctx?.Connection.RemoteIpAddress?.ToString();
    }

    private string? ResolveUserAgent()
    {
        var ua = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }
}
