using Subify.Domain.Entities;

namespace Subify.Application.Features.Admin.Settings;

/// <summary>GET /api/admin/settings response (7.3.1). Secrets never returned in plain text.</summary>
public sealed record SystemSettingsResponse(
    InstanceSettingsDto Instance,
    AiSettingsDto Ai,
    SmtpSettingsDto Smtp,
    DateTimeOffset? UpdatedAt);

public sealed record InstanceSettingsDto(
    string? InstanceName,
    string DefaultLocale,
    string DefaultCurrency,
    string? TimeZoneId,
    bool AllowPublicRegistration,
    bool IsSetupComplete,
    DateTimeOffset? SetupCompletedAt,
    string DefaultApplicationThemeColor,
    bool DefaultDarkTheme);

public sealed record AiSettingsDto(
    string? Provider,
    string? Model,
    string? BaseUrl,
    bool HasApiKey,
    /// <summary>Masked placeholder when key is set; null when unset.</summary>
    string? ApiKeyMasked);

public sealed record SmtpSettingsDto(
    bool Enabled,
    string? Host,
    int? Port,
    string? User,
    bool HasPassword,
    string? PasswordMasked,
    string? FromName,
    string? FromEmail);

public static class SystemSettingsMapper
{
    public const string SecretMask = "••••••••";

    public static SystemSettingsResponse ToResponse(SystemSettings s) =>
        new(
            Instance: new InstanceSettingsDto(
                InstanceName: s.InstanceName,
                DefaultLocale: s.DefaultLocale,
                DefaultCurrency: s.DefaultCurrency,
                TimeZoneId: s.TimeZoneId,
                AllowPublicRegistration: s.AllowPublicRegistration,
                IsSetupComplete: s.IsSetupComplete,
                SetupCompletedAt: s.SetupCompletedAt,
                DefaultApplicationThemeColor: s.DefaultApplicationThemeColor,
                DefaultDarkTheme: s.DefaultDarkTheme),
            Ai: new AiSettingsDto(
                Provider: s.AiProvider,
                Model: s.AiModel,
                BaseUrl: s.AiBaseUrl,
                HasApiKey: s.HasAiConfigured,
                ApiKeyMasked: s.HasAiConfigured ? SecretMask : null),
            Smtp: new SmtpSettingsDto(
                Enabled: s.SmtpEnabled,
                Host: s.SmtpHost,
                Port: s.SmtpPort,
                User: s.SmtpUser,
                HasPassword: !string.IsNullOrWhiteSpace(s.SmtpPassword),
                PasswordMasked: string.IsNullOrWhiteSpace(s.SmtpPassword) ? null : SecretMask,
                FromName: s.SmtpFromName,
                FromEmail: s.SmtpFromEmail),
            UpdatedAt: s.UpdatedAt);
}
