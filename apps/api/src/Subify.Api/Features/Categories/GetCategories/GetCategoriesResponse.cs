using Microsoft.Identity.Client;

namespace Subify.Api.Features.Categories.GetCategories;

public record GetCategoriesResponse(
    Guid Id,
    string Name,
    string Slug,
    string Icon,
    string Color,
    int SortOrder,
    bool IsActive,
    bool IsDefault
);

