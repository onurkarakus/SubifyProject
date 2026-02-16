using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories.GetCategories;

public record GetCategoryQuery(): IRequest<Result<List<GetCategoryResponse>>>;
