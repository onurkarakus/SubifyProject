namespace Subify.Application.Features.Categories;

/// <summary>System category list item (5.1.1).</summary>
public sealed record CategoryResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Icon,
    string? Color,
    int SortOrder);

public sealed record ListCategoriesResponse(IReadOnlyList<CategoryResponse> Data);

/// <summary>User-owned custom category (5.1.2+).</summary>
public sealed record UserCategoryResponse(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    DateTimeOffset CreatedAt);

public sealed record ListUserCategoriesResponse(IReadOnlyList<UserCategoryResponse> Data);
