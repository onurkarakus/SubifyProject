using System.Text.Json;
using Subify.Domain.Entities;

namespace Subify.Application.Features.Subscriptions;

/// <summary>JSON snapshots for subscription activity logs (create/update).</summary>
internal static class SubscriptionActivitySnapshots
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Capture(Subscription entity) =>
        JsonSerializer.Serialize(
            new
            {
                entity.Id,
                entity.Name,
                entity.Price,
                entity.Currency,
                BillingCycle = entity.BillingCycle.ToString(),
                entity.SharedWithCount,
                entity.NextRenewalDate,
                entity.ProviderId,
                entity.CategoryId,
                entity.UserCategoryId,
                entity.Notes,
                entity.Archived
            },
            Json);
}
