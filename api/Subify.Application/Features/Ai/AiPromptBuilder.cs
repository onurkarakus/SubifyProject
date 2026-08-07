using System.Text;
using System.Text.Json;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Services;

namespace Subify.Application.Features.Ai;

/// <summary>
/// Builds server-side analyze prompts with minimal PII (9.1.3).
/// No email/phone; subscription names + financial fields only.
/// </summary>
public static class AiPromptBuilder
{
    public static string BuildSystemPrompt(string locale)
    {
        var lang = string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";
        return lang == "tr"
            ? """
              Sen Subify abonelik tasarruf asistanısın. Kullanıcının aktif abonelik listesine göre kısa, uygulanabilir öneriler üret.
              Yalnızca geçerli JSON döndür (markdown yok). Şema:
              {
                "summary": "string",
                "tips": [
                  {
                    "type": "unused|duplicate|yearly|general",
                    "message": "string",
                    "potentialSaving": number|null,
                    "subscriptionId": "guid|null",
                    "subscriptionName": "string|null"
                  }
                ],
                "estimatedMonthlySaving": number,
                "estimatedYearlySaving": number
              }
              potentialSaving ve estimated* alanları kullanıcının ana para biriminde olmalı.
              Abartılı iddialardan kaçın; veride yoksa uydurma.
              """
            : """
              You are a subscription savings assistant for Subify. Given the user's active subscriptions, produce short actionable tips.
              Return valid JSON only (no markdown). Schema:
              {
                "summary": "string",
                "tips": [
                  {
                    "type": "unused|duplicate|yearly|general",
                    "message": "string",
                    "potentialSaving": number|null,
                    "subscriptionId": "guid|null",
                    "subscriptionName": "string|null"
                  }
                ],
                "estimatedMonthlySaving": number,
                "estimatedYearlySaving": number
              }
              potentialSaving and estimated* must be in the user's main currency.
              Do not invent data not present in the list.
              """;
    }

    public static string BuildUserPrompt(
        IReadOnlyList<Subscription> subscriptions,
        string mainCurrency,
        string locale,
        DateOnly today)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"locale={locale}");
        sb.AppendLine($"mainCurrency={mainCurrency}");
        sb.AppendLine($"today={today:yyyy-MM-dd}");
        sb.AppendLine($"activeCount={subscriptions.Count}");
        sb.AppendLine("subscriptions:");

        foreach (var s in subscriptions.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var monthly = SubscriptionMath.MonthlyEquivalentFromPrice(s.Price, s.SharedWithCount, s.BillingCycle);
            var cycle = s.BillingCycle == BillingCycle.Yearly ? "yearly" : "monthly";
            var category = s.Category?.Slug
                           ?? (s.UserCategory is not null ? $"user:{s.UserCategory.Name}" : "uncategorized");

            sb.Append("- id=").Append(s.Id)
                .Append("; name=").Append(Sanitize(s.Name))
                .Append("; price=").Append(s.Price.ToString("0.##"))
                .Append(' ').Append(s.Currency)
                .Append("; cycle=").Append(cycle)
                .Append("; shared=").Append(s.SharedWithCount)
                .Append("; monthlyEq=").Append(monthly.ToString("0.##"))
                .Append("; category=").Append(Sanitize(category))
                .Append("; nextRenewal=").Append(s.NextRenewalDate.ToString("yyyy-MM-dd"))
                .AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>Compact JSON snapshot for AiSuggestionLog request (no secrets).</summary>
    public static string BuildRequestLogPayload(
        string locale,
        string mainCurrency,
        int activeCount,
        string model,
        string? provider) =>
        JsonSerializer.Serialize(new
        {
            locale,
            mainCurrency,
            activeCount,
            model,
            provider
        });

    public static string BuildReportCommentarySystemPrompt(string locale)
    {
        var lang = string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";
        return lang == "tr"
            ? """
              Sen Subify abonelik harcama asistanısın. Kullanıcının dönem rapor özetine göre kısa, net bir yorum yaz.
              Yalnızca geçerli JSON döndür (markdown yok). Şema:
              {
                "summary": "string (2-4 cümle, ana para biriminde sayılarla)",
                "highlights": ["string", "string"],
                "trend": "up|down|stable",
                "budgetNote": "string|null"
              }
              highlights: en fazla 5 madde; her biri tek cümle, uygulanabilir veya gözlem.
              trend: aylık seriye göre harcama yönü (up=artış, down=düşüş, stable=yatay).
              budgetNote: bütçe yoksa null; varsa bütçe vs son ay harcaması hakkında bir cümle.
              Veride olmayan abonelik veya tutar uydurma. Abartılı iddialardan kaçın.
              """
            : """
              You are a subscription spending assistant for Subify. Write a short clear commentary from the user's period report snapshot.
              Return valid JSON only (no markdown). Schema:
              {
                "summary": "string (2-4 sentences, numbers in main currency)",
                "highlights": ["string", "string"],
                "trend": "up|down|stable",
                "budgetNote": "string|null"
              }
              highlights: at most 5 items; one sentence each, observation or actionable.
              trend: direction of spend from the monthly series (up, down, stable).
              budgetNote: null if no budget; otherwise one sentence on budget vs latest month spend.
              Do not invent subscriptions or amounts not in the snapshot.
              """;
    }

    public static string BuildReportCommentaryUserPrompt(
        int months,
        string mainCurrency,
        string locale,
        decimal? monthlyBudget,
        decimal average,
        decimal latest,
        decimal? momChangePct,
        IReadOnlyList<(string Month, decimal Total)> series,
        IReadOnlyList<(string Category, decimal Total, decimal Percentage, int Count)> topCategories,
        int activeCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"locale={locale}");
        sb.AppendLine($"mainCurrency={mainCurrency}");
        sb.AppendLine($"months={months}");
        sb.AppendLine($"activeSubscriptions={activeCount}");
        sb.AppendLine($"averageMonthly={average:0.##}");
        sb.AppendLine($"latestMonth={latest:0.##}");
        if (momChangePct is { } pct)
        {
            sb.AppendLine($"monthOverMonthChangePct={pct:0.#}");
        }

        if (monthlyBudget is { } budget && budget > 0)
        {
            sb.AppendLine($"monthlyBudget={budget:0.##}");
            sb.AppendLine($"budgetUtilizationPct={(budget > 0 ? latest / budget * 100m : 0m):0.#}");
        }
        else
        {
            sb.AppendLine("monthlyBudget=none");
        }

        sb.AppendLine("monthlySeries:");
        foreach (var (month, total) in series)
        {
            sb.Append("- ").Append(month).Append(": ").Append(total.ToString("0.##")).AppendLine();
        }

        sb.AppendLine("topCategories:");
        if (topCategories.Count == 0)
        {
            sb.AppendLine("- none");
        }
        else
        {
            foreach (var (category, total, percentage, count) in topCategories)
            {
                sb.Append("- name=").Append(Sanitize(category))
                    .Append("; total=").Append(total.ToString("0.##"))
                    .Append("; pct=").Append(percentage.ToString("0.#"))
                    .Append("; count=").Append(count)
                    .AppendLine();
            }
        }

        return sb.ToString();
    }

    public static string BuildReportCommentaryRequestLogPayload(
        string locale,
        string mainCurrency,
        int months,
        int activeCount,
        string model,
        string? provider) =>
        JsonSerializer.Serialize(new
        {
            kind = "report-commentary",
            locale,
            mainCurrency,
            months,
            activeCount,
            model,
            provider
        });

    private static string Sanitize(string value) =>
        value.Replace('\n', ' ').Replace('\r', ' ').Trim();
}
