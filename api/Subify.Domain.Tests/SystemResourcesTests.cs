using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Domain.Tests;

public class SystemResourcesTests
{
    [Fact]
    public void All_has_tr_and_en_for_each_key()
    {
        var groups = SystemResources.All
            .GroupBy(r => (r.PageName, r.Name))
            .ToList();

        Assert.NotEmpty(groups);
        Assert.All(groups, g =>
        {
            var langs = g.Select(x => x.LanguageCode).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { "en", "tr" }, langs);
        });
    }

    [Fact]
    public void All_covers_required_pages_only()
    {
        var pages = SystemResources.All.Select(r => r.PageName).Distinct().OrderBy(p => p).ToArray();

        Assert.Equal(
            new[]
            {
                SystemResources.Pages.Category,
                SystemResources.Pages.Common,
                SystemResources.Pages.Dashboard,
                SystemResources.Pages.Error,
                SystemResources.Pages.Subscription
            },
            pages);
    }

    [Fact]
    public void All_has_no_paywall_or_freemium_keys()
    {
        Assert.DoesNotContain(SystemResources.All, r =>
            r.PageName.Equals("Paywall", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(SystemResources.All, r =>
            r.Name is "subscription_limit" or "premium_required" or "email_not_verified");
    }

    [Fact]
    public void Category_page_covers_all_system_category_slugs()
    {
        var categoryNames = SystemResources.All
            .Where(r => r.PageName == SystemResources.Pages.Category && r.LanguageCode == "tr")
            .Select(r => r.Name)
            .OrderBy(n => n)
            .ToArray();

        var expected = SystemCategories.All.Select(c => c.Slug).OrderBy(s => s).ToArray();
        Assert.Equal(expected, categoryNames);
    }

    [Fact]
    public void Keys_are_unique()
    {
        var distinct = SystemResources.All
            .Select(r => $"{r.PageName}|{r.Name}|{r.LanguageCode}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.Equal(SystemResources.All.Count, distinct);
    }

    [Fact]
    public void Create_normalizes_language_code()
    {
        var resource = Resource.Create("Common", "save", "TR", "Kaydet");

        Assert.Equal("tr", resource.LanguageCode);
        Assert.Equal("Common", resource.PageName);
        Assert.Equal("save", resource.Name);
        Assert.NotEqual(Guid.Empty, resource.Id);
    }
}
