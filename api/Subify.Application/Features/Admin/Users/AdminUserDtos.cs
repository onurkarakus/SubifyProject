using Subify.Application.Features.Subscriptions;

namespace Subify.Application.Features.Admin.Users;

/// <summary>Admin user list/create/patch item (7.1). No subscription payloads (7.1.4).</summary>
public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsLockedOut,
    DateTimeOffset? LockoutEnd,
    bool IsDisabled,
    DateTimeOffset? DisabledAt,
    DateTimeOffset CreatedAt,
    /// <summary>Active subscription count only — not the subscription list (7.1.4).</summary>
    int ActiveSubscriptionCount);

public sealed record ListAdminUsersResponse(
    IReadOnlyList<AdminUserResponse> Data,
    PaginationInfo Pagination);
