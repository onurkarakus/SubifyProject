using Subify.Domain.Entities;
using Subify.Domain.Enums;

namespace Subify.Application.Features.Providers;

/// <summary>Provider catalog item (5.2.1 / 5.2.3).</summary>
public sealed record ProviderResponse(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string Currency,
    decimal? Price,
    decimal? PriceBefore,
    BillingCycle BillingCycle,
    string Region,
    string? SourceUrl,
    DateTimeOffset? LastVerifiedAt,
    bool IsActive)
{
    public static ProviderResponse FromEntity(Provider p) =>
        new(
            p.Id,
            p.Name,
            p.Slug,
            p.LogoUrl,
            p.Currency,
            p.Price,
            p.PriceBefore,
            p.BillingCycle,
            p.Region,
            p.SourceUrl,
            p.LastVerifiedAt,
            p.IsActive);
}

public sealed record ListProvidersResponse(IReadOnlyList<ProviderResponse> Data);
