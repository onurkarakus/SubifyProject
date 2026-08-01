using Subify.Domain.Constants;

namespace Subify.Application.Features.Admin.EmailTemplates;

public sealed record EmailTemplateResponse(
    Guid Id,
    string Name,
    string LanguageCode,
    string Subject,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static EmailTemplateResponse FromEntity(Domain.Entities.EmailTemplates t) =>
        new(
            t.Id,
            t.Name,
            t.LanguageCode,
            t.Subject,
            t.Body,
            t.CreatedAt,
            t.UpdatedAt);
}

public sealed record ListEmailTemplatesResponse(IReadOnlyList<EmailTemplateResponse> Data);

public sealed record PreviewEmailTemplateResponse(
    string Subject,
    string HtmlBody,
    IReadOnlyDictionary<string, string> TokensUsed);

/// <summary>Sample tokens for preview / test send (7.4.2).</summary>
public static class EmailTemplateSampleTokens
{
    public static IReadOnlyDictionary<string, string> For(
        string templateName,
        string appBaseUrl = "http://localhost:3000")
    {
        var bas = appBaseUrl.TrimEnd('/');
        var common = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FullName"] = "Ada Lovelace",
            ["AppUrl"] = bas,
            ["Email"] = "ada@example.com",
            ["ResetUrl"] = $"{bas}/reset-password?email=ada%40example.com&token=sample-token",
            ["SubscriptionName"] = "Netflix",
            ["Amount"] = "149.99",
            ["Currency"] = "TRY",
            ["RenewalDate"] = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)).ToString("yyyy-MM-dd"),
            ["InviterName"] = "Admin",
            ["InstanceName"] = "Subify Lab",
            ["InviteEmail"] = "newuser@example.com",
            ["InviteUrl"] = $"{bas}/accept-invite?token=sample-invite",
            ["Months"] = "6",
            ["AverageMonthly"] = "420.50",
            ["LatestMonth"] = "450.00",
            ["ActiveCount"] = "8",
            ["BudgetLine"] = "500.00 TRY (90% used)",
            ["SeriesHtml"] = "<ul style=\"padding-left:18px;\"><li>2026-01: 400.00</li><li>2026-02: 450.00</li></ul>",
            ["CategoriesHtml"] = "<ul style=\"padding-left:18px;\"><li>Entertainment — 40%</li><li>Software — 25%</li></ul>",
            ["GeneratedAt"] = DateTimeOffset.UtcNow.ToString("u")
        };

        return common;
    }

    public static IReadOnlyList<string> KnownNames => SystemEmailTemplates.SeededNames;
}
