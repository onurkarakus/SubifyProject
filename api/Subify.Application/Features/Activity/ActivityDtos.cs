using Subify.Application.Features.Subscriptions;

namespace Subify.Application.Features.Activity;

/// <summary>Activity feed item (5.4.2).</summary>
public sealed record ActivityItemResponse(
    Guid Id,
    string EntityType,
    Guid? EntityId,
    string Action,
    string Description,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public sealed record ListActivityResponse(
    IReadOnlyList<ActivityItemResponse> Data,
    PaginationInfo Pagination);
