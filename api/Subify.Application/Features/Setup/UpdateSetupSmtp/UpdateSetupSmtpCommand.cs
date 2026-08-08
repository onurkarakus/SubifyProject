using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.UpdateSetupSmtp;

/// <summary>Setup step: persist SMTP settings (3S.5.1). Sending happens later via SmtpEmailSender when enabled.</summary>
public sealed record UpdateSetupSmtpCommand(
    bool? SmtpEnabled,
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUser,
    string? SmtpPassword,
    string? SmtpFromName,
    string? SmtpFromEmail) : IRequest<Result>;

public sealed class UpdateSetupSmtpValidator : AbstractValidator<UpdateSetupSmtpCommand>
{
    public UpdateSetupSmtpValidator()
    {
        RuleFor(x => x.SmtpPort)
            .InclusiveBetween(1, 65535)
            .When(x => x.SmtpPort is not null);

        RuleFor(x => x.SmtpHost).MaximumLength(255).When(x => x.SmtpHost is not null);
        RuleFor(x => x.SmtpUser).MaximumLength(255).When(x => x.SmtpUser is not null);
        RuleFor(x => x.SmtpFromName).MaximumLength(200).When(x => x.SmtpFromName is not null);
        RuleFor(x => x.SmtpFromEmail).MaximumLength(320).When(x => x.SmtpFromEmail is not null);
        RuleFor(x => x.SmtpFromEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.SmtpFromEmail));
    }
}

public sealed class UpdateSetupSmtpHandler : IRequestHandler<UpdateSetupSmtpCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateSetupSmtpHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateSetupSmtpCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return Result.Failure(DomainErrors.Setup.SettingsNotInitialized);
        }

        if (settings.IsSetupComplete)
        {
            return Result.Failure(DomainErrors.Setup.AlreadyComplete);
        }

        settings.UpdateSmtp(
            smtpEnabled: request.SmtpEnabled,
            smtpHost: request.SmtpHost,
            smtpPort: request.SmtpPort,
            smtpUser: request.SmtpUser,
            smtpPassword: request.SmtpPassword,
            smtpFromName: request.SmtpFromName,
            smtpFromEmail: request.SmtpFromEmail);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
