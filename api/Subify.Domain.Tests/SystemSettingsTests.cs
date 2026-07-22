using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Domain.Tests;

public class SystemSettingsTests
{
    [Fact]
    public void CreateDefault_setup_incomplete_and_safe_defaults()
    {
        var settings = SystemSettings.CreateDefault();

        Assert.NotEqual(Guid.Empty, settings.Id);
        Assert.False(settings.IsSetupComplete);
        Assert.Null(settings.SetupCompletedAt);
        Assert.Equal("Subify", settings.InstanceName);
        Assert.Equal(SupportedLocales.Default, settings.DefaultLocale);
        Assert.Equal(SupportedCurrencies.Default, settings.DefaultCurrency);
        Assert.Equal("Europe/Istanbul", settings.TimeZoneId);
        Assert.False(settings.AllowPublicRegistration);
        Assert.False(settings.SmtpEnabled);
        Assert.Null(settings.AiApiKey);
        Assert.Null(settings.SmtpPassword);
        Assert.False(settings.HasAiConfigured);
        Assert.False(settings.HasSmtpConfigured);
    }

    [Fact]
    public void MarkSetupComplete_sets_flag_and_timestamp()
    {
        var settings = SystemSettings.CreateDefault();
        var when = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

        settings.MarkSetupComplete(when);

        Assert.True(settings.IsSetupComplete);
        Assert.Equal(when, settings.SetupCompletedAt);
    }

    [Fact]
    public void UpdateAi_empty_string_clears_key_null_leaves_unchanged()
    {
        var settings = SystemSettings.CreateDefault();
        settings.UpdateAi(aiProvider: "openai", aiApiKey: "sk-test", aiModel: "gpt");

        Assert.True(settings.HasAiConfigured);

        settings.UpdateAi(aiApiKey: null);
        Assert.Equal("sk-test", settings.AiApiKey);

        settings.UpdateAi(aiApiKey: "");
        Assert.Null(settings.AiApiKey);
        Assert.False(settings.HasAiConfigured);
    }
}
