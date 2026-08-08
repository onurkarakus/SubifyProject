using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.EmailTemplates.ListEmailTemplates;

/// <summary>7.4.1 — list all email templates (optional name/lang filter).</summary>
public sealed record ListEmailTemplatesQuery(
    string? Name = null,
    string? LanguageCode = null) : IRequest<Result<ListEmailTemplatesResponse>>;

public sealed class ListEmailTemplatesValidator : AbstractValidator<ListEmailTemplatesQuery>
{
    public ListEmailTemplatesValidator()
    {
        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.LanguageCode).MaximumLength(10).When(x => x.LanguageCode is not null);
    }
}

public sealed class ListEmailTemplatesHandler
    : IRequestHandler<ListEmailTemplatesQuery, Result<ListEmailTemplatesResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListEmailTemplatesHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ListEmailTemplatesResponse>> Handle(
        ListEmailTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ListEmailTemplatesResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<ListEmailTemplatesResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        var query = _db.EmailTemplates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(t => t.Name == name);
        }

        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            var lang = request.LanguageCode.Trim().ToLowerInvariant();
            query = query.Where(t => t.LanguageCode == lang);
        }

        // Materialize then order (SQLite DateTimeOffset ORDER BY issue)
        var rows = await query.ToListAsync(cancellationToken);
        var data = rows
            .OrderBy(t => t.Name)
            .ThenBy(t => t.LanguageCode)
            .Select(EmailTemplateResponse.FromEntity)
            .ToList();

        return Result.Success(new ListEmailTemplatesResponse(data));
    }
}
