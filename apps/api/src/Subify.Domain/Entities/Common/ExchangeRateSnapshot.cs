using Subify.Domain.Entities.Common;

namespace Subify.Domain.Entities.Common;

/// <summary>
/// Periodic snapshot of exchange rates for audit and fallback.
/// Primary source is Redis cache, this is backup/history.
/// </summary>
public sealed class ExchangeRateSnapshot : BaseEntity
{
    /// <summary>
    /// Base currency code (e.g., 'TRY', 'USD'). 
    /// </summary>
    public string BaseCurrency { get; set; } = null!;

    /// <summary>
    /// JSON object with rates: { "USD": 0.029, "EUR": 0.027, "GBP": 0.023 }
    /// </summary>
    public string Rates { get; set; } = null!;

    /// <summary>
    /// API source identifier.
    /// </summary>
    public string Source { get; set; } = "exchangerate-api.comv4/latest/";

    /// <summary>
    /// When rates were fetched from external API.
    /// </summary>
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}