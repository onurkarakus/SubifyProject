using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Providers.UpdateProvider;

public record UpdateProviderCommand(Guid Id, string Name, string Slug, string? Website, string? LogoUrl, bool IsPopular, bool IsActive) : IRequest<Result>;