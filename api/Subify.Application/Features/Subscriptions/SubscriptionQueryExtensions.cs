using Microsoft.EntityFrameworkCore;
using Subify.Domain.Entities;

namespace Subify.Application.Features.Subscriptions;

/// <summary>Shared Include graph for subscription DTOs with nested refs (4.1.10).</summary>
internal static class SubscriptionQueryExtensions
{
    public static IQueryable<Subscription> IncludeDetails(this IQueryable<Subscription> query) =>
        query
            .Include(s => s.Provider)
            .Include(s => s.Category)
            .Include(s => s.UserCategory);
}
