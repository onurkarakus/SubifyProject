namespace Subify.Api.Features.Categories.CreateCategory;

using Subify.Domain.Shared;
using MediatR;

public record CreateCategoryCommand(
    string Slug,
    string Icon,
    string Color,
    int SortOrder,
    string NameTR,
    string NameEN
) : IRequest<Result<CreateCategoryResponse>>;