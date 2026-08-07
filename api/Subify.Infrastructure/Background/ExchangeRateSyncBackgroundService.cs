using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Infrastructure.ExchangeRates;

namespace Subify.Infrastructure.Background;

/// <summary>
/// FX snapshot periodic job (6.2.4 + 8.4). Uses last-known rates when provider is down (6.2.5).
/// </summary>
public sealed class ExchangeRateSyncBackgroundService : IsolatedPeriodicBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ExchangeRateOptions _fxOptions;
    private readonly BackgroundJobsOptions _jobsOptions;
    private readonly ILogger<ExchangeRateSyncBackgroundService> _logger;

    public ExchangeRateSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ExchangeRateOptions> fxOptions,
        IOptions<BackgroundJobsOptions> jobsOptions,
        ILogger<ExchangeRateSyncBackgroundService> logger)
        : base(logger)
    {
        _scopeFactory = scopeFactory;
        _fxOptions = fxOptions.Value;
        _jobsOptions = jobsOptions.Value;
        _logger = logger;
    }

    protected override string JobName => "FxSync";

    protected override bool IsEnabled =>
        _jobsOptions.Enabled && _fxOptions.Enabled;

    protected override TimeSpan Interval => _fxOptions.ResolveSyncInterval();

    protected override TimeSpan StartupDelay =>
        TimeSpan.FromSeconds(Math.Clamp(_fxOptions.StartupDelaySeconds, 0, 300));

    protected override async Task RunIterationAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sync = scope.ServiceProvider.GetRequiredService<IExchangeRateSyncService>();
        var results = await sync.SyncAllAsync(cancellationToken);

        var live = results.Count(r => r.Succeeded && !r.UsedExistingFallback);
        var fallback = results.Count(r => r.UsedExistingFallback);
        var failed = results.Count(r => !r.Succeeded);

        _logger.LogInformation(
            "FxSync cycle done: live={Live}, fallback={Fallback}, failed={Failed}, interval={Interval}",
            live,
            fallback,
            failed,
            Interval);
    }
}
