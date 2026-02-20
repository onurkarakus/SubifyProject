using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Providers.CreateProvider;

public record CreateProviderCommand(string Name, string Slug, string? Website, string? LogoUrl, bool IsPopular) : IRequest<Result<Guid>>;
