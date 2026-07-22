using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Domain.Tests;

public class SystemCategoriesTests
{
    [Fact]
    public void All_has_exactly_ten_system_categories()
    {
        Assert.Equal(10, SystemCategories.All.Count);
    }

    [Fact]
    public void All_contains_expected_slugs_in_task_order()
    {
        var slugs = SystemCategories.All.Select(c => c.Slug).ToArray();

        Assert.Equal(
            new[]
            {
                "streaming", "music", "productivity", "gaming", "shopping",
                "utilities", "education", "health", "cloud", "other"
            },
            slugs);
    }

    [Fact]
    public void All_slugs_are_unique()
    {
        Assert.Equal(
            SystemCategories.All.Count,
            SystemCategories.All.Select(c => c.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Colors_fit_max_length_10()
    {
        Assert.All(SystemCategories.All, c => Assert.True(c.Color.Length <= 10, c.Slug));
    }

    [Fact]
    public void CreateSystem_normalizes_slug_and_marks_default()
    {
        var category = Category.CreateSystem("Streaming", "play-circle", "#E50914", 1);

        Assert.Equal("streaming", category.Slug);
        Assert.True(category.IsDefault);
        Assert.True(category.IsActive);
        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("play-circle", category.Icon);
        Assert.Equal("#E50914", category.Color);
        Assert.Equal(1, category.SortOrder);
    }

    [Theory]
    [InlineData("streaming")]
    [InlineData("OTHER")]
    public void IsSystemSlug_true_for_catalog(string slug) =>
        Assert.True(SystemCategories.IsSystemSlug(slug));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("custom-slug")]
    public void IsSystemSlug_false_for_unknown(string? slug) =>
        Assert.False(SystemCategories.IsSystemSlug(slug));
}
