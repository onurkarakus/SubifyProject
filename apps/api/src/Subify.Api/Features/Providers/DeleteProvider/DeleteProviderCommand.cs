using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Providers.DeleteProvider;

public record DeleteProviderCommand(Guid Id) : IRequest<Result>;
