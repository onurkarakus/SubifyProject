using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Application.Common.Security;
using Subify.Application.Features.Admin.Users;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Invites.CreateInvite;

/// <summary>
/// Create invite (7.2.1). SuperAdmin/Admin. Returns plain token + invite URL for admin to copy.
/// Optional email when SMTP configured (7.2.4 / 15.2.2); create still succeeds if send fails.
/// </summary>
public sealed record CreateInviteCommand(
    string Email,
    int? ExpiryDays = null,
    bool SendEmail = true) : IRequest<Result<CreateInviteResponse>>;

public sealed class CreateInviteValidator : AbstractValidator<CreateInviteCommand>
{
    public const int MinExpiryDays = 1;
    public const int MaxExpiryDays = 90;

    public CreateInviteValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(UserInvite.EmailMaxLength);

        RuleFor(x => x.ExpiryDays!.Value)
            .InclusiveBetween(MinExpiryDays, MaxExpiryDays)
            .When(x => x.ExpiryDays is not null);
    }
}

public sealed class CreateInviteHandler
    : IRequestHandler<CreateInviteCommand, Result<CreateInviteResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppOptions _app;
    private readonly IEmailSender _emailSender;
    private readonly IEmailDeliveryService _delivery;
    private readonly ILogger<CreateInviteHandler> _logger;

    public CreateInviteHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager,
        IOptions<AppOptions> app,
        IEmailSender emailSender,
        IEmailDeliveryService delivery,
        ILogger<CreateInviteHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
        _app = app.Value;
        _emailSender = emailSender;
        _delivery = delivery;
        _logger = logger;
    }

    public async Task<Result<CreateInviteResponse>> Handle(
        CreateInviteCommand request,
        CancellationToken cancellationToken)
    {
        var access = AdminUserAccess.RequireAdminOrAbove(_currentUser);
        if (access.IsFailure)
        {
            return Result.Failure<CreateInviteResponse>(access.Error);
        }

        if (_currentUser.UserId is null)
        {
            return Result.Failure<CreateInviteResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<CreateInviteResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var days = request.ExpiryDays is null
            ? UserInvite.DefaultExpiryDays
            : Math.Clamp(request.ExpiryDays.Value, CreateInviteValidator.MinExpiryDays, CreateInviteValidator.MaxExpiryDays);

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(days);

        // Supersede prior pending invites for this email (single active invite).
        // Materialize then filter: SQLite cannot compare DateTimeOffset in SQL WHERE.
        var priorRows = await _db.UserInvites
            .Where(i => i.Email == email && i.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var old in priorRows.Where(i => !i.IsExpired(now)))
        {
            old.ExpireNow(now);
        }

        var plain = InviteTokenHasher.GeneratePlainText();
        var hash = InviteTokenHasher.Hash(plain);

        var invite = UserInvite.Create(
            email: email,
            tokenHash: hash,
            createdByUserId: _currentUser.UserId.Value,
            expiresAt: expiresAt,
            utcNow: now);

        _db.UserInvites.Add(invite);
        await _db.SaveChangesAsync(cancellationToken);

        var inviteUrl = _app.BuildInviteUrl(plain);
        var emailSent = false;

        // 15.2.2 — best-effort invite email (UI link always returned)
        if (request.SendEmail && await _emailSender.IsConfiguredAsync(cancellationToken))
        {
            try
            {
                var settings = await _db.SystemSettings.AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);

                var inviterName = _currentUser.Email ?? "Admin";
                var inviter = await _db.Users.AsNoTracking()
                    .Where(u => u.Id == _currentUser.UserId.Value)
                    .Select(u => new { u.FullName, u.Locale })
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(inviter?.FullName))
                {
                    inviterName = inviter.FullName;
                }

                var locale = SupportedLocales.Normalize(
                    inviter?.Locale ?? settings?.DefaultLocale);

                var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["InviterName"] = inviterName,
                    ["InstanceName"] = settings?.InstanceName ?? "Subify",
                    ["InviteEmail"] = email,
                    ["InviteUrl"] = inviteUrl,
                    ["AppUrl"] = _app.BaseUrl
                };

                var mail = await _delivery.SendTemplatedAsync(
                    templateName: SystemEmailTemplates.Names.Invite,
                    locale: locale,
                    toEmail: email,
                    tokens: tokens,
                    userId: _currentUser.UserId,
                    relatedEntityId: invite.Id,
                    dedupeKey: $"invite:{invite.Id:N}",
                    cancellationToken: cancellationToken);

                if (mail.IsSuccess)
                {
                    emailSent = true;
                }
                else
                {
                    _logger.LogWarning(
                        "Invite email failed for {Email}: {Code}",
                        email,
                        mail.Error.Code);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invite email threw for {Email}", email);
            }
        }

        return Result.Success(new CreateInviteResponse(
            Id: invite.Id,
            Email: invite.Email,
            Token: plain,
            InviteUrl: inviteUrl,
            ExpiresAt: invite.ExpiresAt,
            CreatedAt: invite.CreatedAt,
            EmailSent: emailSent));
    }
}
