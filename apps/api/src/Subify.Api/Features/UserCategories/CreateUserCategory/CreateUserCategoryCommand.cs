using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.UserCategories.CreateUserCategory;

public record CreateUserCategoryCommand(
    string Name, 
    string? Icon, 
    string? Color, 
    bool IsActive, 
    int SortOrder) : IRequest<Result<Guid>>;
