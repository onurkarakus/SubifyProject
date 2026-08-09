using System.Collections.Generic;
using Serilog.Events;
using Subify.Api.Common.Cors;
using Subify.Api.Common.Logging;
using Subify.Api.Common.OpenApi;
using Subify.Application.Features.Admin.Settings;

namespace Subify.Api.Tests;

/// <summary>Faz 14 — secret masking names, CORS normalize, OpenAPI info constants.</summary>
public class SecurityPolishTests
{
    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("newPassword")]
    [InlineData("smtpPassword")]
    [InlineData("aiApiKey")]
    [InlineData("ApiKey")]
    [InlineData("accessToken")]
    [InlineData("refresh_token")]
    [InlineData("SecretKey")]
    [InlineData("connectionString")]
    public void Sensitive_names_are_detected(string name)
    {
        Assert.True(SensitiveDataDestructuringPolicy.IsSensitiveName(name));
    }

    [Theory]
    [InlineData("email")]
    [InlineData("userId")]
    [InlineData("fullName")]
    [InlineData("tokenHash")]
    [InlineData("daysBeforeRenewal")]
    public void Non_sensitive_names_pass(string name)
    {
        Assert.False(SensitiveDataDestructuringPolicy.IsSensitiveName(name));
    }

    [Fact]
    public void Destructure_masks_password_property_on_dto()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var factory = new CapturingPropertyValueFactory();

        var ok = policy.TryDestructure(
            new { Email = "a@b.com", Password = "Secret1!", FullName = "Ada" },
            factory,
            out var result);

        Assert.True(ok);
        var structure = Assert.IsType<StructureValue>(result);
        var password = structure.Properties.Single(p => p.Name == "Password");
        Assert.Equal(
            SensitiveDataDestructuringPolicy.Redacted,
            Assert.IsType<ScalarValue>(password.Value).Value);
        var email = structure.Properties.Single(p => p.Name == "Email");
        Assert.Equal("a@b.com", Assert.IsType<ScalarValue>(email.Value).Value);
    }

    [Fact]
    public void Settings_secret_mask_is_not_plaintext_placeholder_only()
    {
        Assert.Equal("••••••••", SystemSettingsMapper.SecretMask);
        Assert.DoesNotContain("sk-", SystemSettingsMapper.SecretMask);
    }

    [Fact]
    public void Cors_normalize_trims_and_dedupes_http_origins()
    {
        var origins = CorsServiceExtensions.NormalizeOrigins(
        [
            " https://app.example.com/ ",
            "https://app.example.com",
            "http://localhost:3000",
            "not-a-url",
            "",
            "ftp://bad.example"
        ]);

        Assert.Equal(2, origins.Length);
        Assert.Contains("https://app.example.com", origins);
        Assert.Contains("http://localhost:3000", origins);
    }

    [Fact]
    public void OpenApi_info_constants_are_subify_os()
    {
        Assert.Equal("Subify OS API", OpenApiInfoTransformer.Title);
        Assert.Equal("1.0.0", OpenApiInfoTransformer.Version);
        Assert.Contains("self-hosted", OpenApiInfoTransformer.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JWT", OpenApiInfoTransformer.Description, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Minimal factory: scalars only (enough for destructure policy unit test).</summary>
    private sealed class CapturingPropertyValueFactory : Serilog.Core.ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
        {
            return new ScalarValue(value);
        }
    }
}
