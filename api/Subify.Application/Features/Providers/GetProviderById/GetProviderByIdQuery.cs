using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Providers.GetProviderById;

/// <summary>Get one active catalog provider by id (5.2.2).</summary>
public sealed record GetProviderByIdQuery(Guid Id) : IRequest<Result<ProviderResponse>>;

public sealed class GetProviderByIdHandler
    : IRequestHandler<GetProviderByIdQuery, Result<ProviderResponse>>
{
    private readonly ISubifyDbContext _db;

    public GetProviderByIdHandler(ISubifyDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProviderResponse>> Handle(
        GetProviderByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Soft-deleted rows are filtered out globally → NotFound.
        var provider = await _db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (provider is null)
        {
            return Result.Failure<ProviderResponse>(DomainErrors.ProviderErrors.NotFound);
        }

        if (!provider.IsActive)
        {
            return Result.Failure<ProviderResponse>(DomainErrors.ProviderErrors.InactiveProvider);
        }

        return Result.Success(new ProviderResponse(
            Id: provider.Id,
            Name: provider.Name,
            Slug: provider.Slug,
            LogoUrl: provider.LogoUrl,
            Currency: provider.Currency,
            Price: provider.Price,
            PriceBefore: provider.PriceBefore,
            BillingCycle: provider.BillingCycle,
            Region: provider.Region,
            SourceUrl: provider.SourceUrl,
            LastVerifiedAt: provider.LastVerifiedAt,
            IsActive: provider.IsActive));
    }
}
