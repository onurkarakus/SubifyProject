namespace Subify.Infrastructure.Authentication;

/// <summary>
/// JWT / refresh token options (bound from <c>JwtOptions</c> section — task 3.1.4).
/// </summary>
/// <remarks>
/// Recommended self-host defaults: access <b>15–60 minutes</b>, refresh <b>7 days</b>.
/// Values outside hard limits fall back to defaults at runtime (see <see cref="ResolveAccessTokenLifetime"/>).
/// </remarks>
public class JwtOptions
{
    public const string SectionName = "JwtOptions";

    // --- Access token (minutes) ---
    public const int DefaultAccessTokenMinutes = 60;
    public const int MinAccessTokenMinutes = 5;
    public const int MaxAccessTokenMinutes = 24 * 60; // 24h hard ceiling
    public const int RecommendedMinAccessTokenMinutes = 15;
    public const int RecommendedMaxAccessTokenMinutes = 60;

    // --- Refresh token (days) ---
    public const int DefaultRefreshTokenDays = 7;
    public const int MinRefreshTokenDays = 1;
    public const int MaxRefreshTokenDays = 90;
    public const int RecommendedRefreshTokenDays = 7;

    // --- Validation clock skew (seconds) — task 3.1.5 ---
    /// <summary>Default 30s (tighter than ASP.NET default of 5 minutes).</summary>
    public const int DefaultClockSkewSeconds = 30;
    public const int MinClockSkewSeconds = 0;
    public const int MaxClockSkewSeconds = 300; // 5 minutes hard ceiling

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC signing key; must be ≥ 32 characters.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Access JWT lifetime in minutes (appsettings: <c>JwtOptions:ExpirationInMinutes</c>).
    /// Recommended 15–60; clamped to <see cref="MinAccessTokenMinutes"/>–<see cref="MaxAccessTokenMinutes"/>.
    /// </summary>
    public int ExpirationInMinutes { get; set; } = DefaultAccessTokenMinutes;

    /// <summary>
    /// Refresh token lifetime in days (appsettings: <c>JwtOptions:RefreshTokenExpirationDays</c>).
    /// Recommended 7; clamped to <see cref="MinRefreshTokenDays"/>–<see cref="MaxRefreshTokenDays"/>.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = DefaultRefreshTokenDays;

    /// <summary>
    /// Allowed clock difference when validating <c>nbf</c>/<c>exp</c>
    /// (appsettings: <c>JwtOptions:ClockSkewSeconds</c>). Task 3.1.5.
    /// </summary>
    public int ClockSkewSeconds { get; set; } = DefaultClockSkewSeconds;

    /// <summary>Effective access lifetime used when issuing JWTs.</summary>
    public int ResolveAccessTokenLifetime()
    {
        if (ExpirationInMinutes < MinAccessTokenMinutes || ExpirationInMinutes > MaxAccessTokenMinutes)
        {
            return DefaultAccessTokenMinutes;
        }

        return ExpirationInMinutes;
    }

    /// <summary>Effective refresh lifetime used when issuing refresh tokens.</summary>
    public int ResolveRefreshTokenDays()
    {
        if (RefreshTokenExpirationDays < MinRefreshTokenDays
            || RefreshTokenExpirationDays > MaxRefreshTokenDays)
        {
            return DefaultRefreshTokenDays;
        }

        return RefreshTokenExpirationDays;
    }

    /// <summary>Effective clock skew for JWT validation (task 3.1.5).</summary>
    public TimeSpan ResolveClockSkew()
    {
        if (ClockSkewSeconds < MinClockSkewSeconds || ClockSkewSeconds > MaxClockSkewSeconds)
        {
            return TimeSpan.FromSeconds(DefaultClockSkewSeconds);
        }

        return TimeSpan.FromSeconds(ClockSkewSeconds);
    }

    /// <summary>
    /// Soft check for recommended ranges (logging / ops — does not throw).
    /// </summary>
    public bool IsWithinRecommendedRanges(
        out bool accessOk,
        out bool refreshOk)
    {
        var access = ResolveAccessTokenLifetime();
        var refresh = ResolveRefreshTokenDays();

        accessOk = access is >= RecommendedMinAccessTokenMinutes and <= RecommendedMaxAccessTokenMinutes;
        refreshOk = refresh == RecommendedRefreshTokenDays
                    || (refresh >= MinRefreshTokenDays && refresh <= 30);

        return accessOk && refreshOk;
    }
}

