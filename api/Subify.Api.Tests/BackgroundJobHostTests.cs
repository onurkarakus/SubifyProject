using Microsoft.Extensions.Logging.Abstractions;
using Subify.Infrastructure.Background;
using Subify.Infrastructure.ExchangeRates;

namespace Subify.Api.Tests;

/// <summary>Faz 8.4 — interval parsing, schedule resolve, error isolation.</summary>
public class BackgroundJobHostTests
{
    [Theory]
    [InlineData("1h", 60)]
    [InlineData("30m", 30)]
    [InlineData("90s", 1.5)]
    [InlineData("2d", 2880)]
    [InlineData("1", 60)] // plain number = hours
    [InlineData("1.5h", 90)]
    public void IntervalParser_parses_units(string raw, double expectedMinutes)
    {
        Assert.True(IntervalParser.TryParse(raw, out var ts));
        Assert.Equal(expectedMinutes, ts.TotalMinutes, precision: 5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("abc")]
    [InlineData("0h")]
    [InlineData("-1m")]
    public void IntervalParser_rejects_invalid(string? raw)
    {
        Assert.False(IntervalParser.TryParse(raw, out _));
    }

    [Fact]
    public void IntervalParser_clamps_too_short_and_too_long()
    {
        Assert.True(IntervalParser.TryParse("1s", out var shortTs));
        Assert.Equal(IntervalParser.MinInterval, shortTs);

        Assert.True(IntervalParser.TryParse("30d", out var longTs));
        Assert.Equal(IntervalParser.MaxInterval, longTs);
    }

    [Fact]
    public void ExchangeRateOptions_prefers_SyncInterval_string()
    {
        var opts = new ExchangeRateOptions
        {
            SyncInterval = "30m",
            SyncIntervalHours = 6
        };
        Assert.Equal(TimeSpan.FromMinutes(30), opts.ResolveSyncInterval());
    }

    [Fact]
    public void ExchangeRateOptions_falls_back_to_hours()
    {
        var opts = new ExchangeRateOptions
        {
            SyncInterval = null,
            SyncIntervalHours = 2
        };
        Assert.Equal(TimeSpan.FromHours(2), opts.ResolveSyncInterval());
    }

    [Fact]
    public async Task Isolated_job_survives_iteration_failure_and_runs_again()
    {
        var job = new FlakyJob(failFirst: true, interval: TimeSpan.FromMilliseconds(20));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await job.StartAsync(cts.Token);

        // Wait until at least 2 iterations attempted (1 fail + 1 ok)
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline && job.Attempts < 2)
        {
            await Task.Delay(30);
        }

        await job.StopAsync(CancellationToken.None);

        Assert.True(job.Attempts >= 2, $"expected retries, got {job.Attempts}");
        Assert.True(job.Successes >= 1, "expected at least one success after failure");
        Assert.Equal(1, job.Failures);
    }

    [Fact]
    public async Task Isolated_job_exits_when_disabled()
    {
        var job = new FlakyJob(failFirst: false, interval: TimeSpan.FromHours(1), enabled: false);
        await job.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await job.StopAsync(CancellationToken.None);
        Assert.Equal(0, job.Attempts);
    }

    /// <summary>Test double exposing counters for isolation behavior.</summary>
    private sealed class FlakyJob : IsolatedPeriodicBackgroundService
    {
        private readonly bool _failFirst;
        private readonly TimeSpan _interval;
        private readonly bool _enabled;
        private int _attempts;
        private int _failures;
        private int _successes;

        public FlakyJob(bool failFirst, TimeSpan interval, bool enabled = true)
            : base(NullLogger.Instance)
        {
            _failFirst = failFirst;
            _interval = interval;
            _enabled = enabled;
        }

        public int Attempts => _attempts;
        public int Failures => _failures;
        public int Successes => _successes;

        protected override string JobName => "FlakyTest";
        protected override bool IsEnabled => _enabled;
        protected override TimeSpan Interval => _interval;
        protected override TimeSpan StartupDelay => TimeSpan.Zero;

        protected override Task RunIterationAsync(CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref _attempts);
            if (_failFirst && n == 1)
            {
                Interlocked.Increment(ref _failures);
                throw new InvalidOperationException("intentional first-iteration failure");
            }

            Interlocked.Increment(ref _successes);
            return Task.CompletedTask;
        }
    }
}
