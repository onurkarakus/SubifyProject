using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories.UpdateCategories;

public record UpdateCategoryCommand(
    Guid Id,
    string? Name, // Eğer dolu gelirse Resource tablosu güncellenir
    string Icon,
    string Color,
    int SortOrder,
    bool IsActive
) : IRequest<Result>;