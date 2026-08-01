namespace Subify.Domain.Constants;

/// <summary>Canonical activity entity types and actions (audit trail).</summary>
public static class ActivityLogConstants
{
    public static class EntityTypes
    {
        public const string Subscription = "Subscription";
        public const string Profile = "Profile";
        public const string Auth = "Auth";
        public const string SystemSettings = "SystemSettings";
        public const string AiSuggestion = "AiSuggestion";
    }

    public static class Actions
    {
        public const string SubscriptionCreated = "subscription.created";
        public const string SubscriptionUpdated = "subscription.updated";
        public const string SubscriptionArchived = "subscription.archived";
        public const string SubscriptionReactivated = "subscription.reactivated";
        public const string ProfileUpdated = "profile.updated";
        public const string AuthLogin = "auth.login";
        public const string AuthLogout = "auth.logout";
        public const string SettingsUpdated = "settings.updated";
        /// <summary>SuperAdmin set another user's password (7.5.1 / 3.2.15). Never log the password.</summary>
        public const string AdminPasswordReset = "user.password_reset_by_admin";
        public const string AiAnalyze = "ai.analyze";
        /// <summary>AI period commentary on reports data (optional reports follow-up).</summary>
        public const string AiReportCommentary = "ai.report_commentary";
        /// <summary>User requested email of period report summary (SMTP).</summary>
        public const string ReportEmailSummary = "report.email_summary";
    }
}


