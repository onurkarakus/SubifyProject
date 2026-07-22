using Subify.Domain.Enums;

namespace Subify.Domain.Constants;

/// <summary>
/// Built-in provider catalog seed (task 2.3.6).
/// Prices are reference-only (change over time); LogoUrl left optional for self-host (no CDN).
/// </summary>
public static class SystemProviders
{
    public sealed record Definition(
        string Name,
        string Slug,
        string Currency,
        decimal? Price,
        BillingCycle BillingCycle,
        string Region,
        string? SourceUrl,
        string? LogoUrl = null);

    /// <summary>Initial TR + global catalog (~27 providers).</summary>
    public static readonly IReadOnlyList<Definition> All =
    [
        // Streaming
        new("Netflix", "netflix", "TRY", 149.99m, BillingCycle.Monthly, "TR", "https://www.netflix.com/tr/"),
        new("Disney+", "disney-plus", "TRY", 134.99m, BillingCycle.Monthly, "TR", "https://www.disneyplus.com/tr-tr"),
        new("Amazon Prime Video", "amazon-prime-video", "TRY", 39.00m, BillingCycle.Monthly, "TR", "https://www.primevideo.com/"),
        new("BluTV", "blutv", "TRY", 84.90m, BillingCycle.Monthly, "TR", "https://www.blutv.com/"),
        new("Exxen", "exxen", "TRY", 104.90m, BillingCycle.Monthly, "TR", "https://www.exxen.com/"),
        new("Gain", "gain", "TRY", 49.90m, BillingCycle.Monthly, "TR", "https://www.gain.tv/"),
        new("YouTube Premium", "youtube-premium", "TRY", 79.99m, BillingCycle.Monthly, "TR", "https://www.youtube.com/premium"),
        new("HBO Max", "hbo-max", "USD", 15.99m, BillingCycle.Monthly, "US", "https://www.max.com/"),

        // Music
        new("Spotify", "spotify", "TRY", 59.99m, BillingCycle.Monthly, "TR", "https://www.spotify.com/tr/"),
        new("Apple Music", "apple-music", "TRY", 34.99m, BillingCycle.Monthly, "TR", "https://www.apple.com/tr/apple-music/"),
        new("Deezer", "deezer", "TRY", 29.99m, BillingCycle.Monthly, "TR", "https://www.deezer.com/tr/"),
        new("Fizy", "fizy", "TRY", 24.99m, BillingCycle.Monthly, "TR", "https://fizy.com/"),

        // Productivity
        new("ChatGPT Plus", "chatgpt-plus", "USD", 20.00m, BillingCycle.Monthly, "GLOBAL", "https://chatgpt.com/"),
        new("Microsoft 365", "microsoft-365", "TRY", 129.99m, BillingCycle.Monthly, "TR", "https://www.microsoft.com/tr-tr/microsoft-365"),
        new("Notion", "notion", "USD", 10.00m, BillingCycle.Monthly, "GLOBAL", "https://www.notion.so/"),
        new("Canva Pro", "canva-pro", "TRY", 149.99m, BillingCycle.Monthly, "TR", "https://www.canva.com/"),
        new("Grammarly", "grammarly", "USD", 12.00m, BillingCycle.Monthly, "GLOBAL", "https://www.grammarly.com/"),

        // Gaming
        new("Xbox Game Pass", "xbox-game-pass", "TRY", 109.00m, BillingCycle.Monthly, "TR", "https://www.xbox.com/tr-TR/xbox-game-pass"),
        new("PlayStation Plus", "playstation-plus", "TRY", 159.00m, BillingCycle.Monthly, "TR", "https://www.playstation.com/tr-tr/ps-plus/"),
        new("Nintendo Switch Online", "nintendo-switch-online", "TRY", 69.00m, BillingCycle.Monthly, "TR", "https://www.nintendo.com/"),
        new("EA Play", "ea-play", "TRY", 49.99m, BillingCycle.Monthly, "TR", "https://www.ea.com/ea-play"),

        // Cloud & utilities
        new("iCloud+", "icloud", "TRY", 12.99m, BillingCycle.Monthly, "TR", "https://www.apple.com/tr/icloud/"),
        new("Google One", "google-one", "TRY", 19.99m, BillingCycle.Monthly, "TR", "https://one.google.com/"),
        new("Dropbox Plus", "dropbox-plus", "USD", 11.99m, BillingCycle.Monthly, "GLOBAL", "https://www.dropbox.com/"),
        new("NordVPN", "nordvpn", "USD", 12.99m, BillingCycle.Monthly, "GLOBAL", "https://nordvpn.com/"),

        // Education
        new("Coursera Plus", "coursera-plus", "USD", 59.00m, BillingCycle.Monthly, "GLOBAL", "https://www.coursera.org/"),
        new("Udemy", "udemy", "TRY", 99.00m, BillingCycle.Monthly, "TR", "https://www.udemy.com/"),
        new("Duolingo Plus", "duolingo-plus", "TRY", 399.99m, BillingCycle.Yearly, "TR", "https://www.duolingo.com/")
    ];

    public static bool IsSystemSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && All.Any(p => string.Equals(p.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
}
