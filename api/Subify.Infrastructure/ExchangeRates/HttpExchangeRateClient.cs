using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Services;
using Subify.Domain.Shared;

namespace Subify.Infrastructure.ExchangeRates;

/// <summary>
/// HTTP FX client (6.2.1): open.er-api.com (no key) or exchangerate-api.com v6 (API key).
/// </summary>
public sealed class HttpExchangeRateClient : IExchangeRateClient
{
    public const string HttpClientName = "ExchangeRateProvider";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExchangeRateOptions _options;
    private readonly ILogger<HttpExchangeRateClient> _logger;

    public HttpExchangeRateClient(
        IHttpClientFactory httpClientFactory,
        IOptions<ExchangeRateOptions> options,
        ILogger<HttpExchangeRateClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<ExchangeRateFetchResult>> FetchAsync(
        string baseCurrency,
        IReadOnlyCollection<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default)
    {
        var bas = CurrencyConversion.Normalize(baseCurrency);

        if (_options.UseExchangeRateApiProvider && !_options.HasApiKey)
        {
            _logger.LogWarning("ExchangeRateApi provider selected but ApiKey is missing");
            return Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable);
        }

        var url = BuildUrl(bas);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "FX provider HTTP {Status} for base {Base}",
                    (int)response.StatusCode,
                    bas);
                return Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<ProviderPayload>(stream, JsonOptions, cancellationToken);

            if (payload is null
                || !string.Equals(payload.Result, "success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("FX provider returned non-success for base {Base}: {Error}", bas, payload?.ErrorType);
                return Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable);
            }

            var rawRates = payload.ConversionRates ?? payload.Rates;
            if (rawRates is null || rawRates.Count == 0)
            {
                return Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable);
            }

            var targets = targetCurrencies?
                .Select(CurrencyConversion.Normalize)
                .Where(t => t != bas)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filtered = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var (code, rate) in rawRates)
            {
                var target = CurrencyConversion.Normalize(code);
                if (target == bas || rate <= 0)
                {
                    continue;
                }

                if (targets is not null && !targets.Contains(target))
                {
                    continue;
                }

                filtered[target] = rate;
            }

            if (filtered.Count == 0)
            {
                return Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable);
            }

            var fetchedAt = ParseFetchedAt(payload) ?? DateTimeOffset.UtcNow;
            var source = _options.UseExchangeRateApiProvider ? "exchangerate-api" : "open.er-api";

            return Result.Success(new ExchangeRateFetchResult(
                BaseCurrency: bas,
                Rates: filtered,
                FetchedAt: fetchedAt,
                Source: source));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FX provider request failed for base {Base}", bas);
            return Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable);
        }
    }

    private string BuildUrl(string baseCurrency)
    {
        var root = _options.ResolveBaseUrl();
        if (_options.UseExchangeRateApiProvider)
        {
            // https://v6.exchangerate-api.com/v6/{key}/latest/{BASE}
            return $"{root}{_options.ApiKey!.Trim()}/latest/{baseCurrency}";
        }

        // https://open.er-api.com/v6/latest/{BASE}
        return $"{root}latest/{baseCurrency}";
    }

    private static DateTimeOffset? ParseFetchedAt(ProviderPayload payload)
    {
        if (payload.TimeLastUpdateUnix is > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(payload.TimeLastUpdateUnix.Value);
        }

        if (!string.IsNullOrWhiteSpace(payload.TimeLastUpdateUtc)
            && DateTimeOffset.TryParse(payload.TimeLastUpdateUtc, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }

    private sealed class ProviderPayload
    {
        public string? Result { get; set; }

        [JsonPropertyName("error-type")]
        public string? ErrorType { get; set; }

        [JsonPropertyName("base_code")]
        public string? BaseCode { get; set; }

        /// <summary>exchangerate-api.com</summary>
        [JsonPropertyName("conversion_rates")]
        public Dictionary<string, decimal>? ConversionRates { get; set; }

        /// <summary>open.er-api.com</summary>
        public Dictionary<string, decimal>? Rates { get; set; }

        [JsonPropertyName("time_last_update_unix")]
        public long? TimeLastUpdateUnix { get; set; }

        [JsonPropertyName("time_last_update_utc")]
        public string? TimeLastUpdateUtc { get; set; }
    }
}
