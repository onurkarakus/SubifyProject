using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.EmailTemplates.GetEmailTemplate;

/// <summary>7.4.1 — get one email template by id.</summary>
public sealed record GetEmailTemplateQuery(Guid Id) : IRequest<Result<EmailTemplateResponse>>;

public sealed class GetEmailTemplateHandler
    : IRequestHandler<GetEmailTemplateQuery, Result<EmailTemplateResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetEmailTemplateHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<EmailTemplateResponse>> Handle(
        GetEmailTemplateQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<EmailTemplateResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<EmailTemplateResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        var row = await _db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (row is null)
        {
            return Result.Failure<EmailTemplateResponse>(DomainErrors.ResourceErrors.ResourceNotFound);
        }

        return Result.Success(EmailTemplateResponse.FromEntity(row));
    }
}
