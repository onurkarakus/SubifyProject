namespace Subify.Application.Common.Options;

/// <summary>
/// Public app URLs for invite / reset links. Bound from config section <c>App</c>.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Web UI origin used to build invite links (no trailing slash).
    /// Default: http://localhost:3000
    /// </summary>
    public string PublicWebBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>Path template; <c>{token}</c> is replaced. Default accept-invite page.</summary>
    public string InvitePathTemplate { get; set; } = "/accept-invite?token={token}";

    /// <summary>Password-reset path; <c>{email}</c> and <c>{token}</c> placeholders.</summary>
    public string ResetPasswordPathTemplate { get; set; } =
        "/reset-password?email={email}&token={token}";

    public string BaseUrl => (PublicWebBaseUrl ?? "http://localhost:3000").TrimEnd('/');

    public string BuildInviteUrl(string plainToken)
    {
        var path = string.IsNullOrWhiteSpace(InvitePathTemplate)
            ? "/accept-invite?token={token}"
            : InvitePathTemplate;

        var relative = path.Replace("{token}", Uri.EscapeDataString(plainToken), StringComparison.Ordinal);
        return Combine(relative);
    }

    public string BuildResetPasswordUrl(string email, string resetToken)
    {
        var path = string.IsNullOrWhiteSpace(ResetPasswordPathTemplate)
            ? "/reset-password?email={email}&token={token}"
            : ResetPasswordPathTemplate;

        var relative = path
            .Replace("{email}", Uri.EscapeDataString(email), StringComparison.Ordinal)
            .Replace("{token}", Uri.EscapeDataString(resetToken), StringComparison.Ordinal);
        return Combine(relative);
    }

    private string Combine(string relative)
    {
        if (!relative.StartsWith('/'))
        {
            relative = "/" + relative;
        }

        return BaseUrl + relative;
    }
}
