using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Providers.ListProviders;

/// <summary>
/// Active providers catalog (5.2.1). Optional search (name/slug) and region filter.
/// </summary>
public sealed record ListProvidersQuery(
    string? Search = null,
    string? Region = null) : IRequest<Result<ListProvidersResponse>>;

public sealed class ListProvidersValidator : AbstractValidator<ListProvidersQuery>
{
    public const int SearchMaxLength = 100;
    public const int RegionMaxLength = 20;

    public ListProvidersValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(SearchMaxLength)
            .When(x => x.Search is not null);

        RuleFor(x => x.Region)
            .MaximumLength(RegionMaxLength)
            .When(x => x.Region is not null);
    }
}

public sealed class ListProvidersHandler
    : IRequestHandler<ListProvidersQuery, Result<ListProvidersResponse>>
{
    private readonly ISubifyDbContext _db;

    public ListProvidersHandler(ISubifyDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ListProvidersResponse>> Handle(
        ListProvidersQuery request,
        CancellationToken cancellationToken)
    {
        // Soft-delete filter already excludes deleted; also require IsActive.
        var query = _db.Providers
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term)
                || p.Slug.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim().ToUpperInvariant();
            // GLOBAL matches all regions; also exact region code.
            query = query.Where(p =>
                p.Region == region
                || p.Region == "GLOBAL");
        }

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProviderResponse(
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
                p.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success(new ListProvidersResponse(items));
    }
}
