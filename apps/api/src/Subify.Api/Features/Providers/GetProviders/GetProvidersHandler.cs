using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Providers.GetProviders;

public class GetProvidersHandler : IRequestHandler<GetProvidersQuery, Result<List<GetProviderResponse>>>
{
    private readonly SubifyDbContext subifyDbContext;

    public GetProvidersHandler(SubifyDbContext subifyDbContext)
    {
        this.subifyDbContext = subifyDbContext;
    }

    public async Task<Result<List<GetProviderResponse>>> Handle(GetProvidersQuery request, CancellationToken cancellationToken)
    {
        var providers = await subifyDbContext.Providers
           .AsNoTracking()
           .Where(p => p.IsActive)
           .OrderByDescending(p => p.IsPopular)
           .ThenBy(p => p.Name)
           .Select(p => new GetProviderResponse(p.Id, p.Name, p.Slug, p.Website, p.LogoUrl, p.IsActive, p.IsPopular))
           .ToListAsync(cancellationToken);

        return Result.Success(providers);
    }
}
