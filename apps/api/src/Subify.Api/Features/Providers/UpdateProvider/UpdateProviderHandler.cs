using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Providers.UpdateProvider;

public class UpdateProviderHandler : IRequestHandler<UpdateProviderCommand, Result>
{
    private readonly SubifyDbContext subifyDbContext;

    public UpdateProviderHandler(SubifyDbContext subifyDbContext)
    {
        this.subifyDbContext = subifyDbContext;
    }

    public async Task<Result> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await subifyDbContext.Providers.FindAsync(new object[] { request.Id }, cancellationToken);

        if (provider is null)
        {
            return Result.Failure(DomainErrors.ProviderErrors.NotFound);
        }

        provider.Name = request.Name;
        provider.Slug = request.Slug;
        provider.Website = request.Website;
        provider.LogoUrl = request.LogoUrl;
        provider.IsPopular = request.IsPopular;
        provider.IsActive = request.IsActive;
        provider.UpdatedAt = DateTimeOffset.UtcNow;        
        
        await subifyDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
