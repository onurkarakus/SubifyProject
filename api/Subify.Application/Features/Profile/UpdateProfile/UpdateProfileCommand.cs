using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Profile.UpdateProfile;

/// <summary>
/// Update current user profile preferences (5.3.2).
/// Theme whitelist (5.3.3) and currency set (5.3.4) enforced here.
/// Writes profile.updated activity (5.3.6).
/// </summary>
public sealed record UpdateProfileCommand(
    string FullName,
    string Locale,
    string MainCurrency,
    decimal? MonthlyBudget,
    string ApplicationThemeColor,
    bool DarkTheme) : IRequest<Result<ProfileResponse>>;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(UserProfileConstants.FullNameMaxLength);

        RuleFor(x => x.Locale)
            .NotEmpty()
            .Must(SupportedLocales.IsSupported)
            .WithMessage("Locale must be 'tr' or 'en'.");

        RuleFor(x => x.MainCurrency)
            .NotEmpty()
            .Must(SupportedCurrencies.IsSupported)
            .WithMessage("Currency must be TRY, USD, EUR, or GBP.");

        RuleFor(x => x.ApplicationThemeColor)
            .NotEmpty()
            .Must(ThemeColors.IsSupported)
            .WithMessage("Theme color is not in the supported preset list.");

        RuleFor(x => x.MonthlyBudget)
            .GreaterThan(0)
            .When(x => x.MonthlyBudget is not null)
            .WithMessage("Monthly budget must be positive or null.");
    }
}

public sealed class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, Result<ProfileResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogger _activityLogger;

    public UpdateProfileHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        IActivityLogger activityLogger)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<Result<ProfileResponse>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ProfileResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user is null)
        {
            return Result.Failure<ProfileResponse>(DomainErrors.ProfileErrors.ProfileNotFound);
        }

        if (!SupportedLocales.IsSupported(request.Locale))
        {
            return Result.Failure<ProfileResponse>(DomainErrors.ProfileErrors.InvalidLocale);
        }

        if (!SupportedCurrencies.IsSupported(request.MainCurrency))
        {
            return Result.Failure<ProfileResponse>(DomainErrors.ProfileErrors.InvalidCurrency);
        }

        if (!ThemeColors.IsSupported(request.ApplicationThemeColor))
        {
            return Result.Failure<ProfileResponse>(DomainErrors.ProfileErrors.InvalidTheme);
        }

        if (request.MonthlyBudget is <= 0)
        {
            return Result.Failure<ProfileResponse>(DomainErrors.ProfileErrors.InvalidBudget);
        }

        var oldValues = ProfileActivitySnapshots.Capture(user);

        user.UpdateProfile(
            fullName: request.FullName,
            locale: request.Locale,
            mainCurrency: request.MainCurrency,
            monthlyBudget: request.MonthlyBudget,
            clearMonthlyBudget: request.MonthlyBudget is null,
            applicationThemeColor: request.ApplicationThemeColor,
            darkTheme: request.DarkTheme);

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Failure<ProfileResponse>(
                Error.Failure("PRO_UPDATE", "Profile Update Failed", string.Join("; ", update.Errors.Select(e => e.Description))));
        }

        // 5.3.6 — audit trail (Identity SaveChanges already committed user; activity is separate UoW)
        await _activityLogger.LogAndSaveAsync(
            userId: user.Id,
            entityType: ActivityLogConstants.EntityTypes.Profile,
            action: ActivityLogConstants.Actions.ProfileUpdated,
            description: "Updated profile preferences.",
            entityId: user.Id,
            oldValues: oldValues,
            newValues: ProfileActivitySnapshots.Capture(user),
            cancellationToken: cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);

        return Result.Success(new ProfileResponse(
            Id: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: user.FullName,
            Locale: user.Locale,
            MainCurrency: user.MainCurrency,
            MonthlyBudget: user.MonthlyBudget,
            ApplicationThemeColor: user.ApplicationThemeColor,
            DarkTheme: user.DarkTheme,
            Roles: roles.ToArray(),
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt));
    }
}
