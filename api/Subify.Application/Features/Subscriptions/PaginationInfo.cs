namespace Subify.Application.Features.Subscriptions;

/// <summary>Standard page metadata for list endpoints (4.1.4).</summary>
public sealed record PaginationInfo(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static PaginationInfo Create(int page, int pageSize, int totalItems)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize < 1 ? 1 : pageSize;
        var totalPages = totalItems <= 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)safeSize);

        return new PaginationInfo(safePage, safeSize, totalItems, totalPages);
    }
}
