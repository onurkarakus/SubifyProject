namespace Subify.Domain.Constants;

/// <summary>
/// Built-in email template catalog (task 2.3.8).
/// Stored now; actual SMTP send is Faz 15. <b>No VerifyEmail</b> (email confirm disabled).
/// </summary>
public static class SystemEmailTemplates
{
    public sealed record Definition(
        string Name,
        string LanguageCode,
        string Subject,
        string Body);

    public static class Names
    {
        public const string ResetPassword = "ResetPassword";
        public const string RenewalReminder = "RenewalReminder";
        public const string Invite = "Invite";

        /// <summary>Intentionally not seeded (OS has no email confirmation).</summary>
        public const string VerifyEmail = "VerifyEmail";
    }

    public static readonly IReadOnlyList<string> SeededNames =
    [
        Names.ResetPassword,
        Names.RenewalReminder,
        Names.Invite
    ];

    public static readonly IReadOnlyList<Definition> All = Build();

    private static IReadOnlyList<Definition> Build()
    {
        var list = new List<Definition>(capacity: 6);

        // ResetPassword
        list.Add(new(
            Names.ResetPassword,
            SupportedLocales.Tr,
            "Şifre Sıfırlama - Subify",
            HtmlLayout(
                "Şifre Sıfırlama",
                """
                <p>Merhaba {{FullName}},</p>
                <p>Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
                """,
                "{{ResetUrl}}",
                "Şifremi Sıfırla",
                "<p>Bu link 1 saat geçerlidir.</p><p>Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>",
                footerTr: true)));

        list.Add(new(
            Names.ResetPassword,
            SupportedLocales.En,
            "Password Reset - Subify",
            HtmlLayout(
                "Password Reset",
                """
                <p>Hello {{FullName}},</p>
                <p>Click the button below to reset your password:</p>
                """,
                "{{ResetUrl}}",
                "Reset My Password",
                "<p>This link is valid for 1 hour.</p><p>If you did not request this, you can ignore this email.</p>",
                footerTr: false)));

        // RenewalReminder
        list.Add(new(
            Names.RenewalReminder,
            SupportedLocales.Tr,
            "Ödeme Hatırlatması - {{SubscriptionName}}",
            HtmlLayout(
                "Ödeme Hatırlatması",
                """
                <p>Merhaba {{FullName}},</p>
                <p><strong>{{SubscriptionName}}</strong> aboneliğinizin yenileme tarihi yaklaşıyor:</p>
                <div style="background:#F7F7F7;padding:20px;border-radius:8px;margin:20px 0;">
                  <p style="margin:0;"><strong>Abonelik:</strong> {{SubscriptionName}}</p>
                  <p style="margin:10px 0 0;"><strong>Tutar:</strong> {{Amount}} {{Currency}}</p>
                  <p style="margin:10px 0 0;"><strong>Tarih:</strong> {{RenewalDate}}</p>
                </div>
                """,
                "{{AppUrl}}",
                "Aboneliklerimi Görüntüle",
                "",
                footerTr: true)));

        list.Add(new(
            Names.RenewalReminder,
            SupportedLocales.En,
            "Payment Reminder - {{SubscriptionName}}",
            HtmlLayout(
                "Payment Reminder",
                """
                <p>Hello {{FullName}},</p>
                <p>Your <strong>{{SubscriptionName}}</strong> subscription renewal date is approaching:</p>
                <div style="background:#F7F7F7;padding:20px;border-radius:8px;margin:20px 0;">
                  <p style="margin:0;"><strong>Subscription:</strong> {{SubscriptionName}}</p>
                  <p style="margin:10px 0 0;"><strong>Amount:</strong> {{Amount}} {{Currency}}</p>
                  <p style="margin:10px 0 0;"><strong>Date:</strong> {{RenewalDate}}</p>
                </div>
                """,
                "{{AppUrl}}",
                "View My Subscriptions",
                "",
                footerTr: false)));

        // Invite (link shown in UI always; mail send later in Faz 15)
        list.Add(new(
            Names.Invite,
            SupportedLocales.Tr,
            "Subify daveti - {{InstanceName}}",
            HtmlLayout(
                "Davet",
                """
                <p>Merhaba,</p>
                <p><strong>{{InviterName}}</strong> sizi <strong>{{InstanceName}}</strong> Subify örneğine davet etti.</p>
                <p>Davet e-postası: {{InviteEmail}}</p>
                """,
                "{{InviteUrl}}",
                "Daveti Kabul Et",
                "<p>Bu link sınırlı süre geçerlidir. Daveti siz beklemiyorsanız yok sayabilirsiniz.</p>",
                footerTr: true)));

        list.Add(new(
            Names.Invite,
            SupportedLocales.En,
            "Subify invitation - {{InstanceName}}",
            HtmlLayout(
                "Invitation",
                """
                <p>Hello,</p>
                <p><strong>{{InviterName}}</strong> invited you to the <strong>{{InstanceName}}</strong> Subify instance.</p>
                <p>Invite email: {{InviteEmail}}</p>
                """,
                "{{InviteUrl}}",
                "Accept Invitation",
                "<p>This link expires after a limited time. If you were not expecting this invite, you can ignore it.</p>",
                footerTr: false)));

        return list;
    }

    private static string HtmlLayout(
        string title,
        string bodyHtml,
        string ctaUrl,
        string ctaLabel,
        string afterCtaHtml,
        bool footerTr)
    {
        var footer = footerTr
            ? "Subify OS — Aboneliklerinizi kendi sunucunuzda yönetin"
            : "Subify OS — Manage your subscriptions on your own server";

        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="UTF-8"><title>{title}</title></head>
            <body style="font-family:Arial,sans-serif;line-height:1.6;color:#333;">
              <div style="max-width:600px;margin:0 auto;padding:20px;">
                <h1 style="color:#6B46C1;">{title}</h1>
                {bodyHtml}
                <p style="text-align:center;margin:30px 0;">
                  <a href="{ctaUrl}" style="background:#6B46C1;color:white;padding:12px 30px;text-decoration:none;border-radius:5px;display:inline-block;">{ctaLabel}</a>
                </p>
                {afterCtaHtml}
                <hr style="border:none;border-top:1px solid #eee;margin:30px 0;">
                <p style="color:#888;font-size:12px;">{footer}</p>
              </div>
            </body>
            </html>
            """;
    }
}
