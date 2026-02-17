using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.UserCategories.GetUserCategories;

public record GetUserCategoriesQuery() : IRequest<Result<List<GetUserCategoryResponse>>>;
