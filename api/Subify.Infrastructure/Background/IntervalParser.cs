using System.Globalization;
using System.Text.RegularExpressions;

namespace Subify.Infrastructure.Background;

/// <summary>
/// Parses human-friendly intervals for job schedules (8.4.2).
/// Examples: <c>1h</c>, <c>30m</c>, <c>90s</c>, <c>2d</c>, plain <c>1</c> (= hours).
/// </summary>
public static partial class IntervalParser
{
    private static readonly Regex Pattern = IntervalRegex();

    /// <summary>
    /// Min interval 1 minute (avoid tight loops). Max 7 days.
    /// </summary>
    public static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(7);

    public static bool TryParse(string? value, out TimeSpan interval)
    {
        interval = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var raw = value.Trim();

        // Plain number → hours (backward compat with SyncIntervalHours style)
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var plainHours)
            && plainHours > 0)
        {
            interval = Clamp(TimeSpan.FromHours(plainHours));
            return true;
        }

        var match = Pattern.Match(raw);
        if (!match.Success)
        {
            return false;
        }

        if (!double.TryParse(match.Groups["n"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
            || amount <= 0)
        {
            return false;
        }

        var unit = match.Groups["u"].Value.ToLowerInvariant();
        interval = unit switch
        {
            "s" or "sec" or "secs" or "second" or "seconds" => TimeSpan.FromSeconds(amount),
            "m" or "min" or "mins" or "minute" or "minutes" => TimeSpan.FromMinutes(amount),
            "h" or "hr" or "hrs" or "hour" or "hours" => TimeSpan.FromHours(amount),
            "d" or "day" or "days" => TimeSpan.FromDays(amount),
            _ => TimeSpan.Zero
        };

        if (interval <= TimeSpan.Zero)
        {
            return false;
        }

        interval = Clamp(interval);
        return true;
    }

    public static TimeSpan ParseOrDefault(string? value, TimeSpan fallback)
    {
        return TryParse(value, out var parsed) ? parsed : Clamp(fallback);
    }

    public static TimeSpan Clamp(TimeSpan value)
    {
        if (value < MinInterval)
        {
            return MinInterval;
        }

        if (value > MaxInterval)
        {
            return MaxInterval;
        }

        return value;
    }

    [GeneratedRegex(
        @"^(?<n>\d+(\.\d+)?)\s*(?<u>s|sec|secs|second|seconds|m|min|mins|minute|minutes|h|hr|hrs|hour|hours|d|day|days)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IntervalRegex();
}
