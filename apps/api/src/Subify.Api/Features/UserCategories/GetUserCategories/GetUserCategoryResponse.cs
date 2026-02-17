namespace Subify.Api.Features.UserCategories.GetUserCategories;

public record GetUserCategoryResponse(Guid Id, string Name, string Slug, string? Icon, string? Color, bool IsActive, int SortOrder);
