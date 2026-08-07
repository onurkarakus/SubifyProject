namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Loads latest FX snapshot pairs for conversion (4.3.4).
/// Key: (Base, Target) → Rate meaning 1 Base = Rate Target.
/// </summary>
public interface IExchangeRateLookup
{
    Task<IReadOnlyDictionary<(string From, string To), decimal>> GetLatestRateMapAsync(
        CancellationToken cancellationToken = default);
}
