using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Localization;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Categories.GetSystemCategories;

/// <summary>
/// List active system categories with localized names (5.1.1).
/// Locale: explicit → Accept-Language → user locale → default.
/// </summary>
public sealed record GetSystemCategoriesQuery(
    string? AcceptLanguage = null,
    string? ExplicitLocale = null) : IRequest<Result<ListCategoriesResponse>>;

public sealed class GetSystemCategoriesHandler
    : IRequestHandler<GetSystemCategoriesQuery, Result<ListCategoriesResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryNameLookup _names;

    public GetSystemCategoriesHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        ICategoryNameLookup names)
    {
        _db = db;
        _currentUser = currentUser;
        _names = names;
    }

    public async Task<Result<ListCategoriesResponse>> Handle(
        GetSystemCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var locale = LocaleResolver.Resolve(
            request.ExplicitLocale,
            request.AcceptLanguage,
            _currentUser);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Slug)
            .ToListAsync(cancellationToken);

        var nameMap = await _names.GetNamesAsync(locale, cancellationToken);

        var data = categories
            .Select(c => new CategoryResponse(
                Id: c.Id,
                Slug: c.Slug,
                Name: _names.ResolveName(c.Slug, nameMap),
                Icon: c.Icon,
                Color: c.Color,
                SortOrder: c.SortOrder))
            .ToList();

        return Result.Success(new ListCategoriesResponse(data));
    }
}
