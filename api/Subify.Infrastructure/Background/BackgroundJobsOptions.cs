namespace Subify.Infrastructure.Background;

/// <summary>
/// Cross-job host settings (8.4). Bound from <c>BackgroundJobs</c>.
/// Per-job schedules live on their own options (e.g. <c>ExchangeRates:SyncInterval</c>).
/// </summary>
public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    /// Host kind for ops docs. v1 is always <c>BackgroundService</c> (not Hangfire).
    /// </summary>
    public string HostKind { get; set; } = "BackgroundService";

    /// <summary>Master switch for non-mail background jobs (FX, future cleanup).</summary>
    public bool Enabled { get; set; } = true;
}
