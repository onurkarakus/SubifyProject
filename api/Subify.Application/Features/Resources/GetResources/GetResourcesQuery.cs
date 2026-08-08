using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Localization;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Resources.GetResources;

/// <summary>
/// Localization resource pack with optional delta sync (6.3.1).
/// Full pack cached in memory (6.3.2). <c>since</c> → only changed rows; empty delta → NotModified.
/// </summary>
public sealed record GetResourcesQuery(
    string? Lang = null,
    DateTimeOffset? Since = null,
    string? AcceptLanguage = null) : IRequest<Result<ListResourcesResponse>>;

public sealed class GetResourcesValidator : AbstractValidator<GetResourcesQuery>
{
    public GetResourcesValidator()
    {
        RuleFor(x => x.Lang!)
            .Must(SupportedLocales.IsSupported)
            .WithMessage("Language code must be 'tr' or 'en'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Lang));
    }
}

public sealed class GetResourcesHandler : IRequestHandler<GetResourcesQuery, Result<ListResourcesResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;

    public GetResourcesHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<Result<ListResourcesResponse>> Handle(
        GetResourcesQuery request,
        CancellationToken cancellationToken)
    {
        // Resources are global UI strings — allow authenticated users (and SuperAdmin bootstrap clients).
        // Unauthenticated still blocked by endpoint auth policy / fallback.
        if (!_currentUser.IsAuthenticated)
        {
            return Result.Failure<ListResourcesResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var lang = LocaleResolver.Resolve(request.Lang, request.AcceptLanguage, _currentUser);

        // Full pack: serve from memory cache when no delta filter.
        if (request.Since is null
            && ResourceCache.TryGetFullPack(_cache, lang, out var cached)
            && cached is not null)
        {
            return Result.Success(cached);
        }

        // Materialize then filter: small table; SQLite-safe for DateTimeOffset effective timestamps.
        var rows = await _db.Resources
            .AsNoTracking()
            .Where(r => r.LanguageCode == lang)
            .Select(r => new
            {
                r.PageName,
                r.Name,
                r.Value,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        if (request.Since is { } since)
        {
            rows = rows
                .Where(r => EffectiveAt(r.CreatedAt, r.UpdatedAt) > since)
                .ToList();

            if (rows.Count == 0)
            {
                return Result.Success(new ListResourcesResponse(
                    Data: Array.Empty<ResourceItemResponse>(),
                    LastUpdated: since,
                    NotModified: true));
            }
        }

        var data = rows
            .OrderBy(r => r.PageName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new ResourceItemResponse(r.PageName, r.Name, r.Value))
            .ToList();

        DateTimeOffset? lastUpdated = rows.Count == 0
            ? null
            : rows.Max(r => EffectiveAt(r.CreatedAt, r.UpdatedAt));

        var response = new ListResourcesResponse(
            Data: data,
            LastUpdated: lastUpdated,
            NotModified: false);

        if (request.Since is null)
        {
            ResourceCache.SetFullPack(_cache, lang, response);
        }

        return Result.Success(response);
    }

    private static DateTimeOffset EffectiveAt(DateTimeOffset createdAt, DateTimeOffset? updatedAt) =>
        updatedAt ?? createdAt;
}
