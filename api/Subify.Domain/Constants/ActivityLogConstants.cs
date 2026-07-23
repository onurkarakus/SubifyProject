namespace Subify.Domain.Constants;

/// <summary>Canonical activity entity types and actions (audit trail).</summary>
public static class ActivityLogConstants
{
    public static class EntityTypes
    {
        public const string Subscription = "Subscription";
    }

    public static class Actions
    {
        public const string SubscriptionCreated = "subscription.created";
        public const string SubscriptionUpdated = "subscription.updated";
        public const string SubscriptionArchived = "subscription.archived";
        public const string SubscriptionReactivated = "subscription.reactivated";
    }
}
