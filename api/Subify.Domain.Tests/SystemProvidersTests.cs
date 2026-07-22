using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Enums;

namespace Subify.Domain.Tests;

public class SystemProvidersTests
{
    [Fact]
    public void All_has_expected_minimum_catalog_size()
    {
        Assert.True(SystemProviders.All.Count >= 20, $"Expected >= 20 providers, got {SystemProviders.All.Count}");
    }

    [Fact]
    public void All_includes_core_tr_and_global_providers()
    {
        var slugs = SystemProviders.All.Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("netflix", slugs);
        Assert.Contains("spotify", slugs);
        Assert.Contains("chatgpt-plus", slugs);
        Assert.Contains("xbox-game-pass", slugs);
        Assert.Contains("icloud", slugs);
        Assert.Contains("duolingo-plus", slugs);
    }

    [Fact]
    public void All_slugs_are_unique()
    {
        Assert.Equal(
            SystemProviders.All.Count,
            SystemProviders.All.Select(p => p.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Regions_and_currencies_fit_max_lengths()
    {
        Assert.All(SystemProviders.All, p =>
        {
            Assert.True(p.Region.Length <= 10, p.Slug);
            Assert.True(p.Currency.Length <= 10, p.Slug);
            Assert.True(p.Name.Length <= 200, p.Slug);
            Assert.True(p.Slug.Length <= 100, p.Slug);
        });
    }

    [Fact]
    public void Duolingo_is_yearly()
    {
        var duolingo = SystemProviders.All.Single(p => p.Slug == "duolingo-plus");
        Assert.Equal(BillingCycle.Yearly, duolingo.BillingCycle);
    }

    [Fact]
    public void CreateCatalog_normalizes_and_leaves_logo_null_by_default()
    {
        var provider = Provider.CreateCatalog(
            "Netflix",
            "Netflix",
            "try",
            149.99m,
            BillingCycle.Monthly,
            "tr",
            "https://www.netflix.com/tr/");

        Assert.Equal("netflix", provider.Slug);
        Assert.Equal("TRY", provider.Currency);
        Assert.Equal("TR", provider.Region);
        Assert.Null(provider.LogoUrl);
        Assert.True(provider.IsActive);
        Assert.NotEqual(Guid.Empty, provider.Id);
        Assert.Equal(149.99m, provider.Price);
    }

    [Theory]
    [InlineData("spotify")]
    [InlineData("NETFLIX")]
    public void IsSystemSlug_true_for_catalog(string slug) =>
        Assert.True(SystemProviders.IsSystemSlug(slug));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("my-custom-isp")]
    public void IsSystemSlug_false_for_unknown(string? slug) =>
        Assert.False(SystemProviders.IsSystemSlug(slug));
}
