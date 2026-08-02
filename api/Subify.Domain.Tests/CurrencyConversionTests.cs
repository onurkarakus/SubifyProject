using Subify.Domain.Services;

namespace Subify.Domain.Tests;

/// <summary>Task 4.3.4 — snapshot FX conversion.</summary>
public class CurrencyConversionTests
{
    [Fact]
    public void Convert_same_currency_is_identity()
    {
        var result = CurrencyConversion.Convert(100m, "try", "TRY", new Dictionary<(string, string), decimal>());
        Assert.True(result.WasConverted);
        Assert.Equal(100m, result.Amount);
        Assert.Equal("TRY", result.Currency);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void Convert_uses_direct_rate()
    {
        var rates = new Dictionary<(string, string), decimal>
        {
            [("USD", "TRY")] = 30m
        };

        var result = CurrencyConversion.Convert(10m, "USD", "TRY", rates);
        Assert.True(result.WasConverted);
        Assert.Equal(300m, result.Amount);
        Assert.Equal("TRY", result.Currency);
    }

    [Fact]
    public void Convert_uses_inverse_rate_when_direct_missing()
    {
        var rates = new Dictionary<(string, string), decimal>
        {
            [("TRY", "USD")] = 0.03m // 1 TRY = 0.03 USD → 1 USD ≈ 33.33 TRY
        };

        var result = CurrencyConversion.Convert(3m, "USD", "TRY", rates);
        Assert.True(result.WasConverted);
        Assert.Equal(100m, result.Amount); // 3 / 0.03
    }

    [Fact]
    public void Convert_missing_rate_returns_original_with_warning()
    {
        var result = CurrencyConversion.Convert(50m, "EUR", "TRY", new Dictionary<(string, string), decimal>());
        Assert.False(result.WasConverted);
        Assert.Equal(50m, result.Amount);
        Assert.Equal("EUR", result.Currency);
        Assert.Contains("EUR→TRY", result.Warning);
    }

    [Fact]
    public void BuildRateMap_keeps_latest_per_pair()
    {
        var older = DateTimeOffset.UtcNow.AddDays(-2);
        var newer = DateTimeOffset.UtcNow;

        var map = CurrencyConversion.BuildRateMap(
        [
            ("USD", "TRY", 28m, older),
            ("USD", "TRY", 32m, newer),
            ("EUR", "TRY", 35m, newer)
        ]);

        Assert.Equal(32m, map[("USD", "TRY")]);
        Assert.Equal(35m, map[("EUR", "TRY")]);
    }
}
