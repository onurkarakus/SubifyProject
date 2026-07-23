namespace Subify.Domain.Constants;

public static class SubscriptionConstants
{
    public const int NameMaxLength = 200;
    public const int CurrencyMaxLength = 10;
    public const int NotesMaxLength = 4000;

    public const int PricePrecision = 10;
    public const int PriceScale = 2;

    public const int MinSharedWithCount = 1;

    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int SearchMaxLength = 100;

    public const int DefaultUpcomingDays = 7;
    public const int MinUpcomingDays = 1;
    public const int MaxUpcomingDays = 90;
}
