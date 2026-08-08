using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.EmailTemplates.UpdateEmailTemplate;

/// <summary>7.4.1 — update subject/body of an existing template.</summary>
public sealed record UpdateEmailTemplateCommand(
    Guid Id,
    string Subject,
    string Body) : IRequest<Result<EmailTemplateResponse>>;

public sealed class UpdateEmailTemplateValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(100_000);
    }
}

public sealed class UpdateEmailTemplateHandler
    : IRequestHandler<UpdateEmailTemplateCommand, Result<EmailTemplateResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateEmailTemplateHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<EmailTemplateResponse>> Handle(
        UpdateEmailTemplateCommand request,
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

        var row = await _db.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (row is null)
        {
            return Result.Failure<EmailTemplateResponse>(DomainErrors.ResourceErrors.ResourceNotFound);
        }

        row.UpdateContent(request.Subject, request.Body);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(EmailTemplateResponse.FromEntity(row));
    }
}
