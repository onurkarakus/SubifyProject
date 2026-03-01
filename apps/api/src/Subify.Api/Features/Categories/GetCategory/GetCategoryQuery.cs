using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories.GetCategory;

public record GetCategoryQuery(): IRequest<Result<List<GetCategoryResponse>>>;
