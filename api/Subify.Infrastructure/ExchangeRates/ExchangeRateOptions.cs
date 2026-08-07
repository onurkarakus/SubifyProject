using Subify.Infrastructure.Background;

namespace Subify.Infrastructure.ExchangeRates;

/// <summary>
/// FX provider + sync settings (6.2 / 8.4.2). Bound from config section <c>ExchangeRates</c>
/// and env <c>EXCHANGE_RATE_API_KEY</c> / <c>ExchangeRates__ApiKey</c>.
/// </summary>
public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";

    /// <summary>When false, background job is a no-op; GET still serves last-known DB snapshots.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <c>OpenErApi</c> (default, no key) or <c>ExchangeRateApi</c> (requires ApiKey).
    /// If ApiKey is set, <c>ExchangeRateApi</c> is used automatically.
    /// </summary>
    public string Provider { get; set; } = "OpenErApi";

    /// <summary>API key for exchangerate-api.com (env preferred).</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL including trailing path root.
    /// OpenErApi default: https://open.er-api.com/v6/
    /// ExchangeRateApi default: https://v6.exchangerate-api.com/v6/
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Preferred schedule (8.4.2): <c>1h</c>, <c>30m</c>, <c>90s</c>. Empty → <see cref="SyncIntervalHours"/>.
    /// Env: <c>ExchangeRates__SyncInterval</c>.
    /// </summary>
    public string? SyncInterval { get; set; }

    /// <summary>Fallback hours when <see cref="SyncInterval"/> is empty. Default 1.</summary>
    public int SyncIntervalHours { get; set; } = 1;

    /// <summary>HTTP timeout for provider calls.</summary>
    public int HttpTimeoutSeconds { get; set; } = 15;

    /// <summary>In-memory cache TTL for GET responses (seconds).</summary>
    public int CacheSeconds { get; set; } = 300;

    /// <summary>Optional startup delay before first background sync (seconds).</summary>
    public int StartupDelaySeconds { get; set; } = 15;

    /// <summary>
    /// Bases to sync. Empty → <see cref="Domain.Constants.SupportedCurrencies.All"/>.
    /// </summary>
    public string[] BaseCurrencies { get; set; } = [];

    /// <summary>Resolved period for the hosted FX job.</summary>
    public TimeSpan ResolveSyncInterval()
    {
        if (!string.IsNullOrWhiteSpace(SyncInterval)
            && IntervalParser.TryParse(SyncInterval, out var fromString))
        {
            return fromString;
        }

        return IntervalParser.Clamp(TimeSpan.FromHours(Math.Clamp(SyncIntervalHours, 1, 24 * 7)));
    }

    /// <summary>Legacy alias — prefer <see cref="ResolveSyncInterval"/>.</summary>
    public TimeSpan SyncIntervalTimeSpan => ResolveSyncInterval();

    public TimeSpan HttpTimeout =>
        TimeSpan.FromSeconds(Math.Clamp(HttpTimeoutSeconds, 5, 120));

    public TimeSpan CacheTtl =>
        TimeSpan.FromSeconds(Math.Clamp(CacheSeconds, 0, 3600));

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public bool UseExchangeRateApiProvider =>
        HasApiKey
        || string.Equals(Provider, "ExchangeRateApi", StringComparison.OrdinalIgnoreCase);

    public string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return BaseUrl.TrimEnd('/') + "/";
        }

        return UseExchangeRateApiProvider
            ? "https://v6.exchangerate-api.com/v6/"
            : "https://open.er-api.com/v6/";
    }
}
