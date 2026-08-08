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

namespace Subify.Application.Features.Admin.EmailTemplates.TestSendEmailTemplate;

/// <summary>
/// 7.4.2 — send a rendered sample of the template via SMTP (SuperAdmin).
/// </summary>
public sealed record TestSendEmailTemplateCommand(
    Guid Id,
    string? ToEmail = null,
    IReadOnlyDictionary<string, string>? Tokens = null) : IRequest<Result>;

public sealed class TestSendEmailTemplateValidator : AbstractValidator<TestSendEmailTemplateCommand>
{
    public TestSendEmailTemplateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ToEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ToEmail));
    }
}

public sealed class TestSendEmailTemplateHandler : IRequestHandler<TestSendEmailTemplateCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailSender _emailSender;
    private readonly AppOptions _app;

    public TestSendEmailTemplateHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IEmailSender emailSender,
        IOptions<AppOptions> app)
    {
        _db = db;
        _currentUser = currentUser;
        _emailSender = emailSender;
        _app = app.Value;
    }

    public async Task<Result> Handle(
        TestSendEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        if (!await _emailSender.IsConfiguredAsync(cancellationToken))
        {
            return Result.Failure(DomainErrors.SystemSettingsErrors.SmtpNotConfigured);
        }

        var row = await _db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (row is null)
        {
            return Result.Failure(DomainErrors.ResourceErrors.ResourceNotFound);
        }

        var to = request.ToEmail?.Trim();
        if (string.IsNullOrWhiteSpace(to))
        {
            to = _currentUser.Email;
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            to = await _db.Users.AsNoTracking()
                .Where(u => u.Id == _currentUser.UserId.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            return Result.Failure(DomainErrors.Auth.InvalidEmailFormat);
        }

        var tokens = new Dictionary<string, string>(
            EmailTemplateSampleTokens.For(row.Name, _app.BaseUrl),
            StringComparer.OrdinalIgnoreCase);

        if (request.Tokens is not null)
        {
            foreach (var kv in request.Tokens)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value is not null)
                {
                    tokens[kv.Key.Trim()] = kv.Value;
                }
            }
        }

        var subject = EmailTemplateRenderer.Render(row.Subject, tokens);
        var body = EmailTemplateRenderer.Render(row.Body, tokens);

        // Prefix so SuperAdmin recognizes test mail
        if (!subject.StartsWith("[TEST]", StringComparison.OrdinalIgnoreCase))
        {
            subject = "[TEST] " + subject;
        }

        return await _emailSender.SendAsync(
            new EmailMessage(to, subject, body),
            cancellationToken);
    }
}
