using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.UpdateSetupInstance;

/// <summary>Setup step: instance defaults (3S.3.1). SuperAdmin only while setup open.</summary>
public sealed record UpdateSetupInstanceCommand(
    string? InstanceName,
    string? DefaultLocale,
    string? DefaultCurrency,
    string? TimeZoneId,
    bool? AllowPublicRegistration,
    string? DefaultApplicationThemeColor = null,
    bool? DefaultDarkTheme = null) : IRequest<Result>;

public sealed class UpdateSetupInstanceValidator : AbstractValidator<UpdateSetupInstanceCommand>
{
    public UpdateSetupInstanceValidator()
    {
        RuleFor(x => x.InstanceName)
            .MaximumLength(200)
            .When(x => x.InstanceName is not null);

        RuleFor(x => x.DefaultLocale)
            .Must(l => l is null || SupportedLocales.IsSupported(l))
            .WithMessage("Locale must be tr or en.");

        RuleFor(x => x.DefaultCurrency)
            .Must(c => c is null || SupportedCurrencies.IsSupported(c))
            .WithMessage("Currency must be a supported ISO code (TRY, USD, EUR, GBP).");

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(100)
            .When(x => x.TimeZoneId is not null);

        RuleFor(x => x.DefaultApplicationThemeColor)
            .Must(c => c is null || ThemeColors.IsSupported(c))
            .WithMessage("Theme color is not in the supported preset list.");
    }
}

public sealed class UpdateSetupInstanceHandler : IRequestHandler<UpdateSetupInstanceCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateSetupInstanceHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateSetupInstanceCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return Result.Failure(DomainErrors.Setup.SettingsNotInitialized);
        }

        if (settings.IsSetupComplete)
        {
            return Result.Failure(DomainErrors.Setup.AlreadyComplete);
        }

        settings.UpdateInstance(
            instanceName: request.InstanceName,
            defaultLocale: request.DefaultLocale,
            defaultCurrency: request.DefaultCurrency,
            timeZoneId: request.TimeZoneId,
            allowPublicRegistration: request.AllowPublicRegistration,
            defaultApplicationThemeColor: request.DefaultApplicationThemeColor,
            defaultDarkTheme: request.DefaultDarkTheme);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
