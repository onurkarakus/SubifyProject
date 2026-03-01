using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Subscriptions.GetSubscriptions;

public record GetSubscriptionsQuery() : IRequest<Result<List<GetSubscriptionsResponse>>>;
