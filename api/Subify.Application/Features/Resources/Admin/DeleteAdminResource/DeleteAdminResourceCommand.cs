using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Resources.Admin.DeleteAdminResource;

/// <summary>SuperAdmin: hard-delete i18n resource row (6.3.3).</summary>
public sealed record DeleteAdminResourceCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteAdminResourceHandler : IRequestHandler<DeleteAdminResourceCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;

    public DeleteAdminResourceHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<Result> Handle(DeleteAdminResourceCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var entity = await _db.Resources.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(DomainErrors.ResourceErrors.ResourceNotFound);
        }

        var lang = entity.LanguageCode;
        _db.Resources.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        ResourceCache.Invalidate(_cache, lang);
        return Result.Success();
    }
}
