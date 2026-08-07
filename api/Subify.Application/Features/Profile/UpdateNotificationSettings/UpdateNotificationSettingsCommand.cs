using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Profile.UpdateNotificationSettings;

/// <summary>
/// Update notification prefs (5.3.5 / 15.x).
/// EmailEnabled is stored; renewal reminder job sends only when true and SMTP is configured.
/// </summary>
public sealed record UpdateNotificationSettingsCommand(
    bool? PushEnabled,
    int DaysBeforeRenewal,
    bool? EmailEnabled = null) : IRequest<Result<NotificationSettingsResponse>>;

public sealed class UpdateNotificationSettingsValidator : AbstractValidator<UpdateNotificationSettingsCommand>
{
    public const int MinDays = 0;
    public const int MaxDays = 30;

    public UpdateNotificationSettingsValidator()
    {
        RuleFor(x => x.DaysBeforeRenewal)
            .InclusiveBetween(MinDays, MaxDays)
            .WithMessage($"daysBeforeRenewal must be between {MinDays} and {MaxDays}.");
    }
}

public sealed class UpdateNotificationSettingsHandler
    : IRequestHandler<UpdateNotificationSettingsCommand, Result<NotificationSettingsResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateNotificationSettingsHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationSettingsResponse>> Handle(
        UpdateNotificationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<NotificationSettingsResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        var settings = await _db.NotificationSettings
            .FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = NotificationSetting.CreateDefaults(userId);
            await _db.NotificationSettings.AddAsync(settings, cancellationToken);
        }

        // emailEnabled honored; push optional; days for in-app + email window.
        var push = request.PushEnabled ?? settings.PushEnabled;
        var email = request.EmailEnabled ?? settings.EmailEnabled;
        settings.UpdateSettings(
            emailEnabled: email,
            pushEnabled: push,
            daysBeforeRenewal: request.DaysBeforeRenewal);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new NotificationSettingsResponse(
            EmailEnabled: settings.EmailEnabled,
            PushEnabled: settings.PushEnabled,
            DaysBeforeRenewal: settings.DaysBeforeRenewal));
    }
}
