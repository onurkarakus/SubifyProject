using Subify.Domain.Common;
using Subify.Domain.Constants;

namespace Subify.Domain.Entities;

/// <summary>
/// Instance-wide settings (singleton row). Secrets: AiApiKey, SmtpPassword.
/// SMTP is stored for later EmailSend (Faz 15); sending is not enabled by domain alone.
/// </summary>
public class SystemSettings : BaseEntity
{
    public const int InstanceNameMaxLength = 200;
    public const int TimeZoneIdMaxLength = 100;
    public const int AiProviderMaxLength = 100;
    public const int AiModelMaxLength = 100;
    public const int SmtpHostMaxLength = 255;
    public const int SmtpUserMaxLength = 255;
    public const int SmtpFromNameMaxLength = 200;
    public const int SmtpFromEmailMaxLength = 320;

    // --- Setup ---
    public bool IsSetupComplete { get; private set; }
    public DateTimeOffset? SetupCompletedAt { get; private set; }
    public string? InstanceName { get; private set; }

    // --- Instance defaults ---
    public string DefaultLocale { get; private set; } = SupportedLocales.Default;
    public string DefaultCurrency { get; private set; } = SupportedCurrencies.Default;
    public string? TimeZoneId { get; private set; }
    public bool AllowPublicRegistration { get; private set; }

    // --- AI (BYOK) ---
    public string? AiProvider { get; private set; }
    public string? AiApiKey { get; private set; }
    public string? AiModel { get; private set; }

    // --- SMTP (persist only; send in Faz 15) ---
    public bool SmtpEnabled { get; private set; }
    public string? SmtpHost { get; private set; }
    public int? SmtpPort { get; private set; }
    public string? SmtpUser { get; private set; }
    public string? SmtpPassword { get; private set; }
    public string? SmtpFromName { get; private set; }
    public string? SmtpFromEmail { get; private set; }

    protected SystemSettings()
    {
    }

    /// <summary>Creates the initial singleton row (setup incomplete).</summary>
    public static SystemSettings CreateDefault()
    {
        return new SystemSettings
        {
            Id = GuidGenerator.NewId(),
            IsSetupComplete = false,
            SetupCompletedAt = null,
            InstanceName = "Subify",
            DefaultLocale = SupportedLocales.Default,
            DefaultCurrency = SupportedCurrencies.Default,
            TimeZoneId = "Europe/Istanbul",
            AllowPublicRegistration = false,
            SmtpEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateInstance(
        string? instanceName = null,
        string? defaultLocale = null,
        string? defaultCurrency = null,
        string? timeZoneId = null,
        bool? allowPublicRegistration = null)
    {
        if (instanceName is not null)
        {
            InstanceName = string.IsNullOrWhiteSpace(instanceName)
                ? InstanceName
                : instanceName.Trim();
        }

        if (defaultLocale is not null)
        {
            DefaultLocale = SupportedLocales.Normalize(defaultLocale);
        }

        if (defaultCurrency is not null)
        {
            DefaultCurrency = SupportedCurrencies.Normalize(defaultCurrency);
        }

        if (timeZoneId is not null)
        {
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();
        }

        if (allowPublicRegistration is not null)
        {
            AllowPublicRegistration = allowPublicRegistration.Value;
        }

        Touch();
    }

    /// <summary>
    /// Updates AI settings. Pass <paramref name="aiApiKey"/> null to leave key unchanged;
    /// pass empty string to clear the key.
    /// </summary>
    public void UpdateAi(string? aiProvider = null, string? aiApiKey = null, string? aiModel = null)
    {
        if (aiProvider is not null)
        {
            AiProvider = string.IsNullOrWhiteSpace(aiProvider) ? null : aiProvider.Trim();
        }

        if (aiApiKey is not null)
        {
            AiApiKey = string.IsNullOrWhiteSpace(aiApiKey) ? null : aiApiKey.Trim();
        }

        if (aiModel is not null)
        {
            AiModel = string.IsNullOrWhiteSpace(aiModel) ? null : aiModel.Trim();
        }

        Touch();
    }

    /// <summary>
    /// Updates SMTP settings. Pass <paramref name="smtpPassword"/> null to leave password unchanged;
    /// pass empty string to clear.
    /// </summary>
    public void UpdateSmtp(
        bool? smtpEnabled = null,
        string? smtpHost = null,
        int? smtpPort = null,
        bool clearSmtpPort = false,
        string? smtpUser = null,
        string? smtpPassword = null,
        string? smtpFromName = null,
        string? smtpFromEmail = null)
    {
        if (smtpEnabled is not null)
        {
            SmtpEnabled = smtpEnabled.Value;
        }

        if (smtpHost is not null)
        {
            SmtpHost = string.IsNullOrWhiteSpace(smtpHost) ? null : smtpHost.Trim();
        }

        if (clearSmtpPort)
        {
            SmtpPort = null;
        }
        else if (smtpPort is not null)
        {
            SmtpPort = smtpPort;
        }

        if (smtpUser is not null)
        {
            SmtpUser = string.IsNullOrWhiteSpace(smtpUser) ? null : smtpUser.Trim();
        }

        if (smtpPassword is not null)
        {
            SmtpPassword = string.IsNullOrWhiteSpace(smtpPassword) ? null : smtpPassword;
        }

        if (smtpFromName is not null)
        {
            SmtpFromName = string.IsNullOrWhiteSpace(smtpFromName) ? null : smtpFromName.Trim();
        }

        if (smtpFromEmail is not null)
        {
            SmtpFromEmail = string.IsNullOrWhiteSpace(smtpFromEmail) ? null : smtpFromEmail.Trim();
        }

        Touch();
    }

    public void MarkSetupComplete(DateTimeOffset? completedAt = null)
    {
        IsSetupComplete = true;
        SetupCompletedAt = completedAt ?? DateTimeOffset.UtcNow;
        Touch();
    }

    public void ResetSetupForTestsOnly()
    {
        IsSetupComplete = false;
        SetupCompletedAt = null;
        Touch();
    }

    public bool HasAiConfigured => !string.IsNullOrWhiteSpace(AiApiKey);

    public bool HasSmtpConfigured =>
        SmtpEnabled
        && !string.IsNullOrWhiteSpace(SmtpHost)
        && SmtpPort is > 0
        && !string.IsNullOrWhiteSpace(SmtpFromEmail);

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
