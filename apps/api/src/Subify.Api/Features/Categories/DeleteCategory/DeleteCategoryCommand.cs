using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<Result>;
