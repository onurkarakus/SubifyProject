using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Identity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Security;
using Subify.Application.Common.Validation;
using Subify.Application.Extensions;
using Subify.Domain.Common;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.AcceptInvite;

/// <summary>
/// Accept invite → create User account (7.2.3 / 7.2.5).
/// Works when public registration is disabled. Single-use + expiry enforced.
/// </summary>
public sealed record AcceptInviteCommand(
    string Token,
    string FullName,
    string Password) : IRequest<Result<AcceptInviteResponse>>;

public sealed record AcceptInviteResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Message);

public sealed class AcceptInviteValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(UserProfileConstants.FullNameMaxLength);
        RuleFor(x => x.Password).ApplySubifyPasswordRules();
    }
}

public sealed class AcceptInviteHandler
    : IRequestHandler<AcceptInviteCommand, Result<AcceptInviteResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubifyDbContext _db;

    public AcceptInviteHandler(UserManager<ApplicationUser> userManager, ISubifyDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<Result<AcceptInviteResponse>> Handle(
        AcceptInviteCommand request,
        CancellationToken cancellationToken)
    {
        // Setup must be complete (invite implies an existing admin)
        var settings = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null || !settings.IsSetupComplete)
        {
            return Result.Failure<AcceptInviteResponse>(DomainErrors.Auth.SetupRequired);
        }

        string hash;
        try
        {
            hash = InviteTokenHasher.Hash(request.Token);
        }
        catch (ArgumentException)
        {
            return Result.Failure<AcceptInviteResponse>(DomainErrors.Auth.InvalidInviteToken);
        }

        var invite = await _db.UserInvites
            .FirstOrDefaultAsync(i => i.TokenHash == hash, cancellationToken);

        // 7.2.5 — single-use + expiry (opaque error for used/expired/missing)
        if (invite is null || !invite.IsPending())
        {
            return Result.Failure<AcceptInviteResponse>(DomainErrors.Auth.InvalidInviteToken);
        }

        if (await _userManager.FindByEmailAsync(invite.Email) is not null)
        {
            return Result.Failure<AcceptInviteResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser { Id = GuidGenerator.NewId() };
        user.ApplyRegistrationProfile(request.FullName, invite.Email);
        user.ApplyInstanceDefaults(
            locale: settings.DefaultLocale,
            mainCurrency: settings.DefaultCurrency,
            applicationThemeColor: settings.DefaultApplicationThemeColor,
            darkTheme: settings.DefaultDarkTheme);
        user.EmailConfirmed = true;

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            if (create.IsDuplicateEmailOrUserName())
            {
                return Result.Failure<AcceptInviteResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
            }

            return Result.Failure<AcceptInviteResponse>(create.GetErrors());
        }

        var roleResult = await SuperAdminBootstrap.AssignUserRoleAsync(_userManager, user);
        if (roleResult.IsFailure)
        {
            return Result.Failure<AcceptInviteResponse>(roleResult.Error);
        }

        if (!invite.TryMarkUsed(user.Id))
        {
            // Race: invite became invalid between load and mark — fail closed
            return Result.Failure<AcceptInviteResponse>(DomainErrors.Auth.InvalidInviteToken);
        }

        if (!await _db.NotificationSettings.AnyAsync(n => n.UserId == user.Id, cancellationToken))
        {
            await _db.NotificationSettings.AddAsync(
                NotificationSetting.CreateDefaults(user.Id),
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AcceptInviteResponse(
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Message: "Invite accepted. You can sign in."));
    }
}
