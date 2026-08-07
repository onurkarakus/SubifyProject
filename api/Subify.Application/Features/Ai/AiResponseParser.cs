using System.Text.Json;
using System.Text.RegularExpressions;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Ai;

/// <summary>Parses LLM JSON into analyze DTOs (9.1.4).</summary>
public static partial class AiResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Result<AiAnalyzeResponse> Parse(string content, DateTimeOffset analyzedAt)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure<AiAnalyzeResponse>(Domain.Errors.DomainErrors.AiErrors.ProcessingError);
        }

        var json = ExtractJson(content);
        try
        {
            var dto = JsonSerializer.Deserialize<RawAnalyzeDto>(json, JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Summary))
            {
                return Result.Failure<AiAnalyzeResponse>(Domain.Errors.DomainErrors.AiErrors.ProcessingError);
            }

            var tips = (dto.Tips ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t.Message))
                .Select(t => new AiTipDto(
                    Type: AiTipTypes.Normalize(t.Type),
                    Message: t.Message!.Trim(),
                    PotentialSaving: t.PotentialSaving is > 0
                        ? decimal.Round(t.PotentialSaving.Value, 2, MidpointRounding.AwayFromZero)
                        : null,
                    SubscriptionId: TryGuid(t.SubscriptionId),
                    SubscriptionName: string.IsNullOrWhiteSpace(t.SubscriptionName)
                        ? null
                        : t.SubscriptionName.Trim()))
                .Take(10)
                .ToList();

            var monthly = decimal.Round(Math.Max(0, dto.EstimatedMonthlySaving), 2, MidpointRounding.AwayFromZero);
            var yearly = dto.EstimatedYearlySaving > 0
                ? decimal.Round(dto.EstimatedYearlySaving, 2, MidpointRounding.AwayFromZero)
                : decimal.Round(monthly * 12m, 2, MidpointRounding.AwayFromZero);

            return Result.Success(new AiAnalyzeResponse(
                Summary: dto.Summary.Trim(),
                Tips: tips,
                EstimatedMonthlySaving: monthly,
                EstimatedYearlySaving: yearly,
                AnalyzedAt: analyzedAt));
        }
        catch (JsonException)
        {
            return Result.Failure<AiAnalyzeResponse>(Domain.Errors.DomainErrors.AiErrors.ProcessingError);
        }
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        // Strip ```json fences if the model ignores instructions
        var fence = FenceRegex().Match(trimmed);
        if (fence.Success)
        {
            return fence.Groups[1].Value.Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return trimmed[start..(end + 1)];
        }

        return trimmed;
    }

    public static Result<AiReportCommentaryResponse> ParseReportCommentary(
        string content,
        int months,
        string currency,
        DateTimeOffset generatedAt)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure<AiReportCommentaryResponse>(
                Domain.Errors.DomainErrors.AiErrors.ProcessingError);
        }

        var json = ExtractJson(content);
        try
        {
            var dto = JsonSerializer.Deserialize<RawReportCommentaryDto>(json, JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Summary))
            {
                return Result.Failure<AiReportCommentaryResponse>(
                    Domain.Errors.DomainErrors.AiErrors.ProcessingError);
            }

            var highlights = (dto.Highlights ?? [])
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .Select(h => h.Trim())
                .Take(5)
                .ToList();

            var budgetNote = string.IsNullOrWhiteSpace(dto.BudgetNote)
                ? null
                : dto.BudgetNote.Trim();

            return Result.Success(new AiReportCommentaryResponse(
                Summary: dto.Summary.Trim(),
                Highlights: highlights,
                Trend: AiReportTrends.Normalize(dto.Trend),
                BudgetNote: budgetNote,
                Months: months,
                Currency: currency,
                GeneratedAt: generatedAt));
        }
        catch (JsonException)
        {
            return Result.Failure<AiReportCommentaryResponse>(
                Domain.Errors.DomainErrors.AiErrors.ProcessingError);
        }
    }

    private static Guid? TryGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;

    private sealed class RawAnalyzeDto
    {
        public string? Summary { get; set; }
        public List<RawTipDto>? Tips { get; set; }
        public decimal EstimatedMonthlySaving { get; set; }
        public decimal EstimatedYearlySaving { get; set; }
    }

    private sealed class RawTipDto
    {
        public string? Type { get; set; }
        public string? Message { get; set; }
        public decimal? PotentialSaving { get; set; }
        public string? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }
    }

    private sealed class RawReportCommentaryDto
    {
        public string? Summary { get; set; }
        public List<string>? Highlights { get; set; }
        public string? Trend { get; set; }
        public string? BudgetNote { get; set; }
    }

    [GeneratedRegex(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex FenceRegex();
}
