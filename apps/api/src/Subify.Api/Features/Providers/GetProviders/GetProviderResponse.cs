namespace Subify.Api.Features.Providers.GetProviders;

public record GetProviderResponse(Guid Id, string Name, string Slug, string? Website, string? LogoUrl, bool IsActive, bool IsPopular);
