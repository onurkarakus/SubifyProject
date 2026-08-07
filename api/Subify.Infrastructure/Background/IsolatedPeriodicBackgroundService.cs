using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Subify.Infrastructure.Background;

/// <summary>
/// Shared periodic job host (8.4.1 / 8.4.3).
/// <list type="bullet">
/// <item>v1 decision: <see cref="BackgroundService"/> — not Hangfire/Quartz (self-host simplicity).</item>
/// <item>One failed iteration is logged and the loop continues (error isolation).</item>
/// <item>Schedule via <see cref="Interval"/> (config/env, see <see cref="IntervalParser"/>).</item>
/// </list>
/// </summary>
public abstract class IsolatedPeriodicBackgroundService : BackgroundService
{
    private readonly ILogger _logger;

    protected IsolatedPeriodicBackgroundService(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>For derived jobs that need structured logs in <see cref="RunIterationAsync"/>.</summary>
    protected ILogger Logger => _logger;

    /// <summary>Short name for logs (e.g. <c>FxSync</c>).</summary>
    protected abstract string JobName { get; }

    /// <summary>When false the service exits immediately (no loop).</summary>
    protected abstract bool IsEnabled { get; }

    /// <summary>Delay between successful/failed iterations.</summary>
    protected abstract TimeSpan Interval { get; }

    /// <summary>Optional delay before the first run (let the API finish startup).</summary>
    protected virtual TimeSpan StartupDelay => TimeSpan.Zero;

    /// <summary>One unit of work. Exceptions are caught by the host loop.</summary>
    protected abstract Task RunIterationAsync(CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("Background job {JobName} is disabled", JobName);
            return;
        }

        var startup = StartupDelay;
        if (startup > TimeSpan.Zero)
        {
            _logger.LogInformation(
                "Background job {JobName} waiting {Delay} before first run",
                JobName,
                startup);

            if (!await DelaySafeAsync(startup, stoppingToken))
            {
                return;
            }
        }

        _logger.LogInformation(
            "Background job {JobName} started; interval {Interval}",
            JobName,
            Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunIterationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // 8.4.3 — never let one iteration kill the host process loop
                _logger.LogError(ex, "Background job {JobName} iteration failed; will retry after interval", JobName);
            }

            if (!await DelaySafeAsync(Interval, stoppingToken))
            {
                break;
            }
        }

        _logger.LogInformation("Background job {JobName} stopped", JobName);
    }

    private static async Task<bool> DelaySafeAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
