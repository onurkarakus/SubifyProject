namespace Subify.Application.Features.Resources;

/// <summary>Client-facing resource row (6.3.1).</summary>
public sealed record ResourceItemResponse(
    string PageName,
    string Name,
    string Value);

/// <summary>
/// Delta/full resource pack. When <see cref="NotModified"/> is true the API returns HTTP 304.
/// </summary>
public sealed record ListResourcesResponse(
    IReadOnlyList<ResourceItemResponse> Data,
    DateTimeOffset? LastUpdated,
    bool NotModified = false);

/// <summary>Admin resource row with identity and audit (6.3.3).</summary>
public sealed record AdminResourceResponse(
    Guid Id,
    string PageName,
    string Name,
    string LanguageCode,
    string Value,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ListAdminResourcesResponse(
    IReadOnlyList<AdminResourceResponse> Data);
