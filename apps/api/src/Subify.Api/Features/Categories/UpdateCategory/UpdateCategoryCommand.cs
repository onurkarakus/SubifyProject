using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string? Name,
    string Icon,
    string Color,
    int SortOrder,
    bool IsActive
) : IRequest<Result>;