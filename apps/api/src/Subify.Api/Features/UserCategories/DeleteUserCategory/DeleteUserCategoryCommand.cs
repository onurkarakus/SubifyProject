using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.UserCategories.DeleteUserCategory;

public record DeleteUserCategoryCommand(Guid Id) : IRequest<Result>;
