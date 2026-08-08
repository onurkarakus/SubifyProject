using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Email;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.EmailTemplates.PreviewEmailTemplate;

/// <summary>
/// 7.4.2 — render template with sample (or provided) tokens; no SMTP send.
/// </summary>
public sealed record PreviewEmailTemplateCommand(
    Guid Id,
    IReadOnlyDictionary<string, string>? Tokens = null) : IRequest<Result<PreviewEmailTemplateResponse>>;

public sealed class PreviewEmailTemplateValidator : AbstractValidator<PreviewEmailTemplateCommand>
{
    public PreviewEmailTemplateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class PreviewEmailTemplateHandler
    : IRequestHandler<PreviewEmailTemplateCommand, Result<PreviewEmailTemplateResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly AppOptions _app;

    public PreviewEmailTemplateHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IOptions<AppOptions> app)
    {
        _db = db;
        _currentUser = currentUser;
        _app = app.Value;
    }

    public async Task<Result<PreviewEmailTemplateResponse>> Handle(
        PreviewEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<PreviewEmailTemplateResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<PreviewEmailTemplateResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        var row = await _db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (row is null)
        {
            return Result.Failure<PreviewEmailTemplateResponse>(DomainErrors.ResourceErrors.ResourceNotFound);
        }

        var tokens = MergeTokens(row.Name, request.Tokens);
        var subject = EmailTemplateRenderer.Render(row.Subject, tokens);
        var body = EmailTemplateRenderer.Render(row.Body, tokens);

        return Result.Success(new PreviewEmailTemplateResponse(subject, body, tokens));
    }

    private IReadOnlyDictionary<string, string> MergeTokens(
        string templateName,
        IReadOnlyDictionary<string, string>? overrideTokens)
    {
        var map = new Dictionary<string, string>(
            EmailTemplateSampleTokens.For(templateName, _app.BaseUrl),
            StringComparer.OrdinalIgnoreCase);

        if (overrideTokens is not null)
        {
            foreach (var kv in overrideTokens)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value is not null)
                {
                    map[kv.Key.Trim()] = kv.Value;
                }
            }
        }

        return map;
    }
}
