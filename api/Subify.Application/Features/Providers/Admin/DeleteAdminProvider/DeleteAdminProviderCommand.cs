using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Providers.Admin.DeleteAdminProvider;

/// <summary>
/// SuperAdmin: soft-delete provider (5.2.3).
/// Conflict if any active (non-archived) subscription references it.
/// </summary>
public sealed record DeleteAdminProviderCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteAdminProviderHandler : IRequestHandler<DeleteAdminProviderCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteAdminProviderHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteAdminProviderCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var entity = await _db.Providers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure(DomainErrors.ProviderErrors.NotFound);
        }

        var hasActiveSubs = await _db.Subscriptions
            .AsNoTracking()
            .AnyAsync(s => s.ProviderId == entity.Id && !s.Archived, cancellationToken);

        if (hasActiveSubs)
        {
            return Result.Failure(DomainErrors.ProviderErrors.HasActiveSubscriptions);
        }

        entity.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
