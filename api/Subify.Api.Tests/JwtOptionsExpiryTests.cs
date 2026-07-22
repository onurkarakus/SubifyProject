using System.Text.Json;
using Subify.Infrastructure.Authentication;

namespace Subify.Api.Tests;

/// <summary>
/// Task 3.1.4 — access/refresh expiry config resolution and appsettings presence.
/// </summary>
public class JwtOptionsExpiryTests
{
    [Theory]
    [InlineData(15, 15)]
    [InlineData(30, 30)]
    [InlineData(60, 60)]
    [InlineData(5, 5)]
    [InlineData(1440, 1440)]
    public void ResolveAccessTokenLifetime_accepts_valid_range(int configured, int expected)
    {
        var options = new JwtOptions { ExpirationInMinutes = configured };
        Assert.Equal(expected, options.ResolveAccessTokenLifetime());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(1441)]
    public void ResolveAccessTokenLifetime_falls_back_outside_hard_range(int configured)
    {
        var options = new JwtOptions { ExpirationInMinutes = configured };
        Assert.Equal(JwtOptions.DefaultAccessTokenMinutes, options.ResolveAccessTokenLifetime());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(30, 30)]
    [InlineData(90, 90)]
    public void ResolveRefreshTokenDays_accepts_valid_range(int configured, int expected)
    {
        var options = new JwtOptions { RefreshTokenExpirationDays = configured };
        Assert.Equal(expected, options.ResolveRefreshTokenDays());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(91)]
    public void ResolveRefreshTokenDays_falls_back_outside_hard_range(int configured)
    {
        var options = new JwtOptions { RefreshTokenExpirationDays = configured };
        Assert.Equal(JwtOptions.DefaultRefreshTokenDays, options.ResolveRefreshTokenDays());
    }

    [Fact]
    public void IsWithinRecommendedRanges_true_for_defaults()
    {
        var options = new JwtOptions
        {
            ExpirationInMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        Assert.True(options.IsWithinRecommendedRanges(out var accessOk, out var refreshOk));
        Assert.True(accessOk);
        Assert.True(refreshOk);
    }

    [Fact]
    public void Appsettings_declare_both_expiry_keys()
    {
        var root = FindRepoRoot();
        var baseJson = Path.Combine(root, "api", "Subify.Api", "appsettings.json");
        var devJson = Path.Combine(root, "api", "Subify.Api", "appsettings.Development.json");

        Assert.True(File.Exists(baseJson));
        Assert.True(File.Exists(devJson));

        AssertHasExpiry(baseJson);
        AssertHasExpiry(devJson);
    }

    private static void AssertHasExpiry(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var jwt = doc.RootElement.GetProperty("JwtOptions");
        Assert.True(jwt.TryGetProperty("ExpirationInMinutes", out var access));
        Assert.True(jwt.TryGetProperty("RefreshTokenExpirationDays", out var refresh));
        Assert.InRange(access.GetInt32(), JwtOptions.MinAccessTokenMinutes, JwtOptions.MaxAccessTokenMinutes);
        Assert.InRange(refresh.GetInt32(), JwtOptions.MinRefreshTokenDays, JwtOptions.MaxRefreshTokenDays);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "api", "Subify.Api"))
                && Directory.Exists(Path.Combine(dir.FullName, "docker")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
