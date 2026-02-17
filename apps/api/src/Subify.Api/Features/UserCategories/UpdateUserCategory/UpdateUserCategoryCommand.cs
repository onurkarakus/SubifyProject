using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.UserCategories.UpdateUserCategory;

public record UpdateUserCategoryCommand(
    Guid Id, 
    string Name, 
    string? Icon, 
    string? Color, 
    bool IsActive, 
    int SortOrder) : IRequest<Result>;
