using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Resources.Admin.ListAdminResources;

/// <summary>SuperAdmin: list resources with optional lang/page filters (6.3.3).</summary>
public sealed record ListAdminResourcesQuery(
    string? Lang = null,
    string? PageName = null) : IRequest<Result<ListAdminResourcesResponse>>;

public sealed class ListAdminResourcesHandler
    : IRequestHandler<ListAdminResourcesQuery, Result<ListAdminResourcesResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListAdminResourcesHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ListAdminResourcesResponse>> Handle(
        ListAdminResourcesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<ListAdminResourcesResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!string.IsNullOrWhiteSpace(request.Lang) && !SupportedLocales.IsSupported(request.Lang))
        {
            return Result.Failure<ListAdminResourcesResponse>(DomainErrors.ResourceErrors.InvalidLanguage);
        }

        var query = _db.Resources.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Lang))
        {
            var lang = SupportedLocales.Normalize(request.Lang);
            query = query.Where(r => r.LanguageCode == lang);
        }

        if (!string.IsNullOrWhiteSpace(request.PageName))
        {
            var page = request.PageName.Trim();
            query = query.Where(r => r.PageName == page);
        }

        var rows = await query.ToListAsync(cancellationToken);

        var data = rows
            .OrderBy(r => r.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.PageName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new AdminResourceResponse(
                r.Id,
                r.PageName,
                r.Name,
                r.LanguageCode,
                r.Value,
                r.CreatedAt,
                r.UpdatedAt))
            .ToList();

        return Result.Success(new ListAdminResourcesResponse(data));
    }
}
