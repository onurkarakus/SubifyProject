using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Profile.GetNotificationSettings;

/// <summary>Read notification prefs (companion to 5.3.5 PUT).</summary>
public sealed record GetNotificationSettingsQuery : IRequest<Result<NotificationSettingsResponse>>;

public sealed class GetNotificationSettingsHandler
    : IRequestHandler<GetNotificationSettingsQuery, Result<NotificationSettingsResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationSettingsHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationSettingsResponse>> Handle(
        GetNotificationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<NotificationSettingsResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var settings = await _db.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken);

        if (settings is null)
        {
            // Lazy defaults without persist (PUT will create row).
            return Result.Success(new NotificationSettingsResponse(
                EmailEnabled: false,
                PushEnabled: false,
                DaysBeforeRenewal: NotificationSetting.DefaultDaysBeforeRenewal));
        }

        return Result.Success(new NotificationSettingsResponse(
            EmailEnabled: settings.EmailEnabled,
            PushEnabled: settings.PushEnabled,
            DaysBeforeRenewal: settings.DaysBeforeRenewal));
    }
}
