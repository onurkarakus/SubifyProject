using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;

namespace Subify.Infrastructure.Background;

/// <summary>
/// 15.3.1 — periodic host for renewal reminder emails.
/// 15.3.2 — dedupe handled inside <see cref="IRenewalReminderService"/>.
/// </summary>
public sealed class RenewalReminderBackgroundService : IsolatedPeriodicBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<BackgroundJobsOptions> _jobs;
    private readonly IOptionsMonitor<EmailJobsOptions> _emailJobs;

    public RenewalReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<BackgroundJobsOptions> jobs,
        IOptionsMonitor<EmailJobsOptions> emailJobs,
        ILogger<RenewalReminderBackgroundService> logger)
        : base(logger)
    {
        _scopeFactory = scopeFactory;
        _jobs = jobs;
        _emailJobs = emailJobs;
    }

    protected override string JobName => "RenewalReminder";

    protected override bool IsEnabled =>
        _jobs.CurrentValue.Enabled && _emailJobs.CurrentValue.RenewalRemindersEnabled;

    protected override TimeSpan Interval =>
        IntervalParser.ParseOrDefault(
            _emailJobs.CurrentValue.RenewalReminderInterval,
            TimeSpan.FromHours(6));

    protected override TimeSpan StartupDelay =>
        TimeSpan.FromSeconds(Math.Clamp(_emailJobs.CurrentValue.StartupDelaySeconds, 0, 300));

    protected override async Task RunIterationAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IRenewalReminderService>();
        var count = await runner.ProcessDueRemindersAsync(cancellationToken);

        if (count > 0)
        {
            Logger.LogInformation("{JobName}: processed {Count} reminder send(s)", JobName, count);
        }
    }
}

/// <summary>Bound from <c>EmailJobs</c> config section.</summary>
public sealed class EmailJobsOptions
{
    public const string SectionName = "EmailJobs";

    public bool RenewalRemindersEnabled { get; set; } = true;

    /// <summary>How often to scan (e.g. 6h, 1d).</summary>
    public string RenewalReminderInterval { get; set; } = "6h";

    public int StartupDelaySeconds { get; set; } = 45;
}
