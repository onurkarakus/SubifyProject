using Subify.Application.Features.Subscriptions;

namespace Subify.Application.Features.Ai;

/// <summary>POST /api/ai/analyze response (9.2.1).</summary>
public sealed record AiAnalyzeResponse(
    string Summary,
    IReadOnlyList<AiTipDto> Tips,
    decimal EstimatedMonthlySaving,
    decimal EstimatedYearlySaving,
    DateTimeOffset AnalyzedAt);

public sealed record AiTipDto(
    string Type,
    string Message,
    decimal? PotentialSaving = null,
    Guid? SubscriptionId = null,
    string? SubscriptionName = null);

/// <summary>GET /api/ai/history item (9.2.2).</summary>
public sealed record AiHistoryItemResponse(
    Guid Id,
    string Summary,
    decimal EstimatedMonthlySaving,
    decimal EstimatedYearlySaving,
    DateTimeOffset CreatedAt);

public sealed record ListAiHistoryResponse(
    IReadOnlyList<AiHistoryItemResponse> Data,
    PaginationInfo Pagination);

/// <summary>GET /api/ai/history/{id} — full stored analyze payload (9.2.2 detail).</summary>
public sealed record AiHistoryDetailResponse(
    Guid Id,
    string Summary,
    IReadOnlyList<AiTipDto> Tips,
    decimal EstimatedMonthlySaving,
    decimal EstimatedYearlySaving,
    DateTimeOffset AnalyzedAt,
    DateTimeOffset CreatedAt);

/// <summary>Canonical tip types from the model (9.1.4).</summary>
public static class AiTipTypes
{
    public const string Unused = "unused";
    public const string Duplicate = "duplicate";
    public const string Yearly = "yearly";
    public const string General = "general";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Unused, Duplicate, Yearly, General
    };

    public static string Normalize(string? type) =>
        !string.IsNullOrWhiteSpace(type) && All.Contains(type.Trim())
            ? type.Trim().ToLowerInvariant()
            : General;
}

/// <summary>POST /api/ai/report-commentary — narrative over period report aggregates.</summary>
public sealed record AiReportCommentaryResponse(
    string Summary,
    IReadOnlyList<string> Highlights,
    string Trend,
    string? BudgetNote,
    int Months,
    string Currency,
    DateTimeOffset GeneratedAt);

/// <summary>Allowed trend labels from the model for report commentary.</summary>
public static class AiReportTrends
{
    public const string Up = "up";
    public const string Down = "down";
    public const string Stable = "stable";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Up, Down, Stable
    };

    public static string Normalize(string? trend) =>
        !string.IsNullOrWhiteSpace(trend) && All.Contains(trend.Trim())
            ? trend.Trim().ToLowerInvariant()
            : Stable;
}
