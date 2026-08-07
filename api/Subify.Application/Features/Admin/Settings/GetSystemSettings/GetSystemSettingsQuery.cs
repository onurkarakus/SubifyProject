using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Settings.GetSystemSettings;

/// <summary>
/// SuperAdmin: read instance + AI + SMTP settings (7.3.1).
/// AI key and SMTP password are masked.
/// </summary>
public sealed record GetSystemSettingsQuery : IRequest<Result<SystemSettingsResponse>>;

public sealed class GetSystemSettingsHandler
    : IRequestHandler<GetSystemSettingsQuery, Result<SystemSettingsResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSystemSettingsHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<SystemSettingsResponse>> Handle(
        GetSystemSettingsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result.Failure<SystemSettingsResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<SystemSettingsResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        var settings = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            return Result.Failure<SystemSettingsResponse>(DomainErrors.SystemSettingsErrors.NotFound);
        }

        return Result.Success(SystemSettingsMapper.ToResponse(settings));
    }
}
