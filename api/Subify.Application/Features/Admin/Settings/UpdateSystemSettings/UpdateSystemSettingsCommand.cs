using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Settings.UpdateSystemSettings;

/// <summary>
/// SuperAdmin: partial update of instance / AI / SMTP (7.3.2).
/// Secrets: omit or null = leave unchanged; empty string = clear; non-empty = set.
/// Writes audit log without secret values (7.3.5).
/// </summary>
public sealed record UpdateSystemSettingsCommand(
    // Instance
    string? InstanceName = null,
    string? DefaultLocale = null,
    string? DefaultCurrency = null,
    string? TimeZoneId = null,
    bool? AllowPublicRegistration = null,
    string? DefaultApplicationThemeColor = null,
    bool? DefaultDarkTheme = null,
    // AI
    string? AiProvider = null,
    string? AiApiKey = null,
    string? AiModel = null,
    string? AiBaseUrl = null,
    // SMTP
    bool? SmtpEnabled = null,
    string? SmtpHost = null,
    int? SmtpPort = null,
    bool ClearSmtpPort = false,
    string? SmtpUser = null,
    string? SmtpPassword = null,
    string? SmtpFromName = null,
    string? SmtpFromEmail = null) : IRequest<Result<SystemSettingsResponse>>;

public sealed class UpdateSystemSettingsValidator : AbstractValidator<UpdateSystemSettingsCommand>
{
    public UpdateSystemSettingsValidator()
    {
        RuleFor(x => x.InstanceName)
            .MaximumLength(SystemSettings.InstanceNameMaxLength)
            .When(x => x.InstanceName is not null);

        RuleFor(x => x.DefaultLocale)
            .Must(l => l is null || SupportedLocales.IsSupported(l))
            .WithMessage("Locale must be tr or en.");

        RuleFor(x => x.DefaultCurrency)
            .Must(c => c is null || SupportedCurrencies.IsSupported(c))
            .WithMessage("Currency must be TRY, USD, EUR, or GBP.");

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(SystemSettings.TimeZoneIdMaxLength)
            .When(x => x.TimeZoneId is not null);

        RuleFor(x => x.DefaultApplicationThemeColor)
            .Must(c => c is null || ThemeColors.IsSupported(c))
            .WithMessage("Theme color is not in the supported preset list.");

        RuleFor(x => x.AiProvider)
            .MaximumLength(SystemSettings.AiProviderMaxLength)
            .When(x => x.AiProvider is not null);

        RuleFor(x => x.AiModel)
            .MaximumLength(SystemSettings.AiModelMaxLength)
            .When(x => x.AiModel is not null);

        RuleFor(x => x.AiBaseUrl)
            .MaximumLength(SystemSettings.AiBaseUrlMaxLength)
            .When(x => x.AiBaseUrl is not null);

        RuleFor(x => x.SmtpPort)
            .InclusiveBetween(1, 65535)
            .When(x => x.SmtpPort is not null);

        RuleFor(x => x.SmtpHost)
            .MaximumLength(SystemSettings.SmtpHostMaxLength)
            .When(x => x.SmtpHost is not null);

        RuleFor(x => x.SmtpUser)
            .MaximumLength(SystemSettings.SmtpUserMaxLength)
            .When(x => x.SmtpUser is not null);

        RuleFor(x => x.SmtpFromName)
            .MaximumLength(SystemSettings.SmtpFromNameMaxLength)
            .When(x => x.SmtpFromName is not null);

        RuleFor(x => x.SmtpFromEmail)
            .MaximumLength(SystemSettings.SmtpFromEmailMaxLength)
            .When(x => x.SmtpFromEmail is not null);

        RuleFor(x => x.SmtpFromEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.SmtpFromEmail));
    }
}

public sealed class UpdateSystemSettingsHandler
    : IRequestHandler<UpdateSystemSettingsCommand, Result<SystemSettingsResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogger _activityLogger;

    public UpdateSystemSettingsHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IActivityLogger activityLogger)
    {
        _db = db;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<Result<SystemSettingsResponse>> Handle(
        UpdateSystemSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<SystemSettingsResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<SystemSettingsResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return Result.Failure<SystemSettingsResponse>(DomainErrors.SystemSettingsErrors.NotFound);
        }

        var oldSnapshot = BuildAuditSnapshot(settings);

        settings.UpdateInstance(
            instanceName: request.InstanceName,
            defaultLocale: request.DefaultLocale,
            defaultCurrency: request.DefaultCurrency,
            timeZoneId: request.TimeZoneId,
            allowPublicRegistration: request.AllowPublicRegistration,
            defaultApplicationThemeColor: request.DefaultApplicationThemeColor,
            defaultDarkTheme: request.DefaultDarkTheme);

        settings.UpdateAi(
            aiProvider: request.AiProvider,
            aiApiKey: request.AiApiKey,
            aiModel: request.AiModel,
            aiBaseUrl: request.AiBaseUrl);

        settings.UpdateSmtp(
            smtpEnabled: request.SmtpEnabled,
            smtpHost: request.SmtpHost,
            smtpPort: request.SmtpPort,
            clearSmtpPort: request.ClearSmtpPort,
            smtpUser: request.SmtpUser,
            smtpPassword: request.SmtpPassword,
            smtpFromName: request.SmtpFromName,
            smtpFromEmail: request.SmtpFromEmail);

        var newSnapshot = BuildAuditSnapshot(settings);

        // 7.3.5 — audit without secret values
        await _activityLogger.LogAsync(
            userId: _currentUser.UserId.Value,
            entityType: ActivityLogConstants.EntityTypes.SystemSettings,
            action: ActivityLogConstants.Actions.SettingsUpdated,
            description: "System settings updated.",
            entityId: settings.Id,
            oldValues: JsonSerializer.Serialize(oldSnapshot, JsonOptions),
            newValues: JsonSerializer.Serialize(newSnapshot, JsonOptions),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(SystemSettingsMapper.ToResponse(settings));
    }

    /// <summary>Public fields + secret presence flags only — never plain AI/SMTP secrets.</summary>
    private static object BuildAuditSnapshot(SystemSettings s) => new
    {
        s.InstanceName,
        s.DefaultLocale,
        s.DefaultCurrency,
        s.TimeZoneId,
        s.AllowPublicRegistration,
        s.DefaultApplicationThemeColor,
        s.DefaultDarkTheme,
        AiProvider = s.AiProvider,
        AiModel = s.AiModel,
        AiBaseUrl = s.AiBaseUrl,
        HasAiApiKey = s.HasAiConfigured,
        SmtpEnabled = s.SmtpEnabled,
        SmtpHost = s.SmtpHost,
        SmtpPort = s.SmtpPort,
        SmtpUser = s.SmtpUser,
        HasSmtpPassword = !string.IsNullOrWhiteSpace(s.SmtpPassword),
        SmtpFromName = s.SmtpFromName,
        SmtpFromEmail = s.SmtpFromEmail
    };
}
