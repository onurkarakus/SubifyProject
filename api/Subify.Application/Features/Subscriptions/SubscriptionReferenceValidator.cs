using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions;

/// <summary>
/// Shared provider/category/user-category checks for create/update (4.1.11 / 4.1.12).
/// </summary>
internal static class SubscriptionReferenceValidator
{
    public static async Task<Result> ValidateAsync(
        ISubifyDbContext db,
        Guid userId,
        Guid? providerId,
        Guid? categoryId,
        Guid? userCategoryId,
        CancellationToken cancellationToken)
    {
        if (providerId is { } pid)
        {
            // Soft-deleted / inactive providers are treated as not usable (4.1.11).
            var provider = await db.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pid, cancellationToken);

            if (provider is null || !provider.IsActive)
            {
                return Result.Failure(DomainErrors.Subscription.ProviderNotActive);
            }
        }

        if (categoryId is { } cid)
        {
            var category = await db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cid, cancellationToken);

            if (category is null || !category.IsActive)
            {
                return Result.Failure(DomainErrors.Subscription.CategoryNotFound);
            }
        }

        if (userCategoryId is { } ucid)
        {
            var userCategory = await db.UserCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == ucid, cancellationToken);

            if (userCategory is null)
            {
                return Result.Failure(DomainErrors.Subscription.CategoryNotFound);
            }

            if (userCategory.UserId != userId)
            {
                return Result.Failure(DomainErrors.Subscription.SubscriptionAccessDenied);
            }
        }

        return Result.Success();
    }
}
