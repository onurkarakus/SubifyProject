using Microsoft.AspNetCore.Identity;
using Subify.Application.Extensions;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Common.Identity;

/// <summary>
/// Race-safe SuperAdmin bootstrap helpers (tasks 3.3.1 / 3.3.2).
/// </summary>
public static class SuperAdminBootstrap
{
    public static async Task<bool> HasAnySuperAdminAsync(UserManager<ApplicationUser> userManager)
    {
        var supers = await userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
        return supers.Count > 0;
    }

    /// <summary>
    /// Assigns SuperAdmin only when none exist; re-checks after assign (race-safe demotion).
    /// </summary>
    public static async Task<Result> TryAssignFirstSuperAdminAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        if (await HasAnySuperAdminAsync(userManager))
        {
            return Result.Failure(DomainErrors.Auth.SuperAdminAlreadyExists);
        }

        var add = await userManager.AddToRoleAsync(user, AppRoles.SuperAdmin);
        if (!add.Succeeded)
        {
            return Result.Failure(add.GetErrors());
        }

        // Concurrent bootstrap: keep a single SuperAdmin
        var supers = await userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
        if (supers.Count > 1)
        {
            await userManager.RemoveFromRoleAsync(user, AppRoles.SuperAdmin);
            var demote = await userManager.AddToRoleAsync(user, AppRoles.User);
            if (!demote.Succeeded)
            {
                return Result.Failure(demote.GetErrors());
            }

            return Result.Failure(DomainErrors.Auth.SuperAdminBootstrapRace);
        }

        return Result.Success();
    }

    /// <summary>Public register path: always User (task 3.3.2).</summary>
    public static async Task<Result> AssignUserRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        var result = await userManager.AddToRoleAsync(user, AppRoles.User);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.GetErrors());
    }
}
