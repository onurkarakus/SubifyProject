using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Providers;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Providers.CreateProvider;

public class CreateProviderHandler : IRequestHandler<CreateProviderCommand, Result<Guid>>
{
    private readonly SubifyDbContext subifyDbContext;

    public CreateProviderHandler(SubifyDbContext subifyDbContext)
    {
        this.subifyDbContext = subifyDbContext;
    }

    public async Task<Result<Guid>> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        var exists = await subifyDbContext.Providers.AnyAsync(p => p.Name == request.Name, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(DomainErrors.ProviderErrors.DuplicateName);
        }

        var slugExist = await subifyDbContext.Providers.AnyAsync(p => p.Slug == request.Slug, cancellationToken);

        if (slugExist)
        {
            return Result.Failure<Guid>(DomainErrors.ProviderErrors.DuplicateSlug);
        }

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            Website = request.Website,
            LogoUrl = request.LogoUrl,
            IsPopular = request.IsPopular,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        subifyDbContext.Providers.Add(provider);
        await subifyDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(provider.Id);
    }
}
