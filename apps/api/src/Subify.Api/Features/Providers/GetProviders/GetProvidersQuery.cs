using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Providers.GetProviders;

public record GetProvidersQuery() : IRequest<Result<List<GetProviderResponse>>>;
