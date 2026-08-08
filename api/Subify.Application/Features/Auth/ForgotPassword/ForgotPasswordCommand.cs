using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.ForgotPassword;

/// <summary>
/// 15.2.1 / 3.2.7 — request password reset email.
/// Always succeeds for valid email format (no account enumeration).
/// Sends only when user exists and SMTP is configured.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IEmailDeliveryService _delivery;
    private readonly AppOptions _app;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IEmailDeliveryService delivery,
        IOptions<AppOptions> app,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _delivery = delivery;
        _app = app.Value;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        // Always OK path for clients (enumeration-safe)
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || user.IsDisabled)
        {
            _logger.LogInformation("Forgot-password: no active user for supplied email");
            return Result.Success();
        }

        if (!await _emailSender.IsConfiguredAsync(cancellationToken))
        {
            _logger.LogWarning("Forgot-password: SMTP not configured; email not sent");
            return Result.Success();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = _app.BuildResetPasswordUrl(user.Email ?? email, token);
        var locale = SupportedLocales.Normalize(user.Locale);

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FullName"] = user.FullName ?? user.Email ?? email,
            ["ResetUrl"] = resetUrl,
            ["AppUrl"] = _app.BaseUrl,
            ["Email"] = user.Email ?? email
        };

        var send = await _delivery.SendTemplatedAsync(
            templateName: SystemEmailTemplates.Names.ResetPassword,
            locale: locale,
            toEmail: user.Email ?? email,
            tokens: tokens,
            userId: user.Id,
            relatedEntityId: user.Id,
            dedupeKey: null,
            cancellationToken: cancellationToken);

        if (send.IsFailure)
        {
            _logger.LogWarning(
                "Forgot-password send failed for user {UserId}: {Code}",
                user.Id,
                send.Error.Code);
            // Still success to client — do not leak SMTP/user state
        }

        return Result.Success();
    }
}
