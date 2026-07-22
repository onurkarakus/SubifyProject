namespace Subify.Domain.Constants;

/// <summary>
/// Built-in system category catalog (task 2.3.5).
/// Display names come from Resources (PageName=Category, Name=slug) — not stored on Category.
/// </summary>
public static class SystemCategories
{
    public sealed record Definition(
        string Slug,
        string Icon,
        string Color,
        int SortOrder);

    public const string Streaming = "streaming";
    public const string Music = "music";
    public const string Productivity = "productivity";
    public const string Gaming = "gaming";
    public const string Shopping = "shopping";
    public const string Utilities = "utilities";
    public const string Education = "education";
    public const string Health = "health";
    public const string Cloud = "cloud";
    public const string Other = "other";

    /// <summary>All default system categories (seed order by <see cref="Definition.SortOrder"/>).</summary>
    public static readonly IReadOnlyList<Definition> All =
    [
        new(Streaming, "play-circle", "#E50914", 1),
        new(Music, "music-note", "#1DB954", 2),
        new(Productivity, "briefcase", "#0078D4", 3),
        new(Gaming, "gamepad", "#9146FF", 4),
        new(Shopping, "shopping-cart", "#FF9900", 5),
        new(Utilities, "tool", "#6C757D", 6),
        new(Education, "book-open", "#00A86B", 7),
        new(Health, "heart", "#FF6B6B", 8),
        new(Cloud, "cloud", "#4285F4", 9),
        new(Other, "more-horizontal", "#8E8E93", 99)
    ];

    public static bool IsSystemSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && All.Any(c => string.Equals(c.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
}
