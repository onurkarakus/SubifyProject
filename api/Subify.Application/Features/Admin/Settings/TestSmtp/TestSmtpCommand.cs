using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Settings.TestSmtp;

/// <summary>15.3.3 / 7.3.3 — SuperAdmin sends a test email via configured SMTP.</summary>
public sealed record TestSmtpCommand(string? ToEmail = null) : IRequest<Result>;

public sealed class TestSmtpValidator : AbstractValidator<TestSmtpCommand>
{
    public TestSmtpValidator()
    {
        RuleFor(x => x.ToEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ToEmail));
    }
}

public sealed class TestSmtpHandler : IRequestHandler<TestSmtpCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailSender _emailSender;

    public TestSmtpHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IEmailSender emailSender)
    {
        _db = db;
        _currentUser = currentUser;
        _emailSender = emailSender;
    }

    public async Task<Result> Handle(TestSmtpCommand request, CancellationToken cancellationToken)
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

        var to = request.ToEmail?.Trim();
        if (string.IsNullOrWhiteSpace(to))
        {
            to = _currentUser.Email;
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            var user = await _db.Users.AsNoTracking()
                .Where(u => u.Id == _currentUser.UserId.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(cancellationToken);
            to = user;
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            return Result.Failure(DomainErrors.Auth.InvalidEmailFormat);
        }

        // Single singleton row — avoid SQLite DateTimeOffset ORDER BY
        var settings = await _db.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var instance = settings?.InstanceName ?? "Subify";

        return await _emailSender.SendAsync(
            new EmailMessage(
                ToEmail: to,
                Subject: $"[Subify OS] SMTP test — {instance}",
                HtmlBody: $"""
                    <p>This is a test email from <strong>{System.Net.WebUtility.HtmlEncode(instance)}</strong>.</p>
                    <p>If you received this, SMTP is configured correctly.</p>
                    <p style="color:#888;font-size:12px;">Subify OS · SMTP test</p>
                    """),
            cancellationToken);
    }
}
