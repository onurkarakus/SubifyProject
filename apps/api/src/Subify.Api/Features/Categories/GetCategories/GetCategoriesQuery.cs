using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories.GetCategories;

public record GetCategoriesQuery(): IRequest<Result<List<GetCategoriesResponse>>>;
