using Microsoft.AspNetCore.Identity;
using Subify.Domain.Entities;

namespace Subify.Application.Features.Admin.Users;

internal static class AdminUserMapper
{
    public static async Task<AdminUserResponse> ToResponseAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        int activeSubscriptionCount,
        CancellationToken cancellationToken = default)
    {
        var roles = await userManager.GetRolesAsync(user);
        var locked = await userManager.IsLockedOutAsync(user);

        return new AdminUserResponse(
            Id: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: user.FullName,
            Roles: roles.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToArray(),
            IsLockedOut: locked,
            LockoutEnd: user.LockoutEnd,
            IsDisabled: user.IsDisabled,
            DisabledAt: user.DisabledAt,
            CreatedAt: user.CreatedAt,
            ActiveSubscriptionCount: activeSubscriptionCount);
    }
}
