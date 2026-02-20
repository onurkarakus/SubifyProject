using Azure.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Providers.DeleteProvider;

public class DeleteProviderHandler : IRequestHandler<DeleteProviderCommand, Result>
{
    private readonly SubifyDbContext subifyDbContext;

    public DeleteProviderHandler(SubifyDbContext subifyDbContext)
    {
        this.subifyDbContext = subifyDbContext;
    }

    public async Task<Result> Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await subifyDbContext.Providers.FindAsync(new object[] { request.Id }, cancellationToken);

        if (provider is null)
        {
            return Result.Failure(DomainErrors.ProviderErrors.NotFound);
        }

        var isUsed = await subifyDbContext.Subscriptions.AnyAsync(s => s.ProviderId == request.Id);

        if (isUsed)
        {
            return Result.Failure(DomainErrors.ProviderErrors.HasActiveSubscriptions);
        }

        subifyDbContext.Providers.Remove(provider);

        await subifyDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
