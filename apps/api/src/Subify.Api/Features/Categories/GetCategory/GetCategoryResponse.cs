
namespace Subify.Api.Features.Categories.GetCategory;

public record GetCategoryResponse(
    Guid Id,
    string Slug,
    string Icon,
    string Color,
    int SortOrder,
    bool IsActive,
    bool IsDefault
);

