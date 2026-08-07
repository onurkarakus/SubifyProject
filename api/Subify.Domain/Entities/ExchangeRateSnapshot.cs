using System;
using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class ExchangeRateSnapshot : BaseEntity
{
    public string BaseCurrency { get; private set; } = string.Empty;
    public string TargetCurrency { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public DateTimeOffset FetchedAt { get; private set; }

    protected ExchangeRateSnapshot() { } // EF Core için

    public ExchangeRateSnapshot(string baseCurrency, string targetCurrency, decimal rate, string source, DateTimeOffset fetchedAt)
    {
        Id = GuidGenerator.NewId();
        CreatedAt = DateTimeOffset.UtcNow;
        BaseCurrency = baseCurrency;
        TargetCurrency = targetCurrency;
        Rate = rate;
        Source = source;
        FetchedAt = fetchedAt;
    }
}