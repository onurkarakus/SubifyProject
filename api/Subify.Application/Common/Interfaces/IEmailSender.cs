using Subify.Domain.Shared;

namespace Subify.Application.Common.Interfaces;

/// <summary>Outbound email. Implementations: SMTP when configured, otherwise SET_003.</summary>
public interface IEmailSender
{
    /// <summary>True when SystemSettings has SMTP enabled + host/port/from.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send one message. When SMTP is off/incomplete → <c>SET_003</c>.
    /// Does not throw for SMTP transport failures — returns failure Result.
    /// </summary>
    Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string HtmlBody,
    string? ToName = null);
