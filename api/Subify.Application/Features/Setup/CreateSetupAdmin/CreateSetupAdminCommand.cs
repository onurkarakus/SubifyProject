using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Identity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Extensions;
using Subify.Domain.Common;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.CreateSetupAdmin;

/// <summary>First SuperAdmin (3S.2.1 / 3.3.1). Optional tokens for wizard session (3S.2.2).</summary>
public sealed record CreateSetupAdminCommand(
    string FullName,
    string Email,
    string Password) : IRequest<Result<CreateSetupAdminResponse>>;

public sealed record CreateSetupAdminResponse(
    string UserId,
    string Email,
    string FullName,
    string Role,
    string? AccessToken,
    string? RefreshToken,
    DateTime? Expiration);

public sealed class CreateSetupAdminHandler
    : IRequestHandler<CreateSetupAdminCommand, Result<CreateSetupAdminResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubifyDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateSetupAdminHandler(
        UserManager<ApplicationUser> userManager,
        ISubifyDbContext db,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<CreateSetupAdminResponse>> Handle(
        CreateSetupAdminCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is { IsSetupComplete: true })
        {
            return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Setup.AlreadyComplete);
        }

        if (await SuperAdminBootstrap.HasAnySuperAdminAsync(_userManager))
        {
            return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Auth.SuperAdminAlreadyExists);
        }

        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser { Id = GuidGenerator.NewId() };
        user.ApplyRegistrationProfile(fullName, email);
        user.EmailConfirmed = true;

        if (settings is not null)
        {
            user.ApplyInstanceDefaults(
                locale: settings.DefaultLocale,
                mainCurrency: settings.DefaultCurrency,
                applicationThemeColor: settings.DefaultApplicationThemeColor,
                darkTheme: settings.DefaultDarkTheme);
        }

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            if (create.IsDuplicateEmailOrUserName())
            {
                return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
            }

            return Result.Failure<CreateSetupAdminResponse>(create.GetErrors());
        }

        var roleAssign = await SuperAdminBootstrap.TryAssignFirstSuperAdminAsync(_userManager, user);
        if (roleAssign.IsFailure)
        {
            if (roleAssign.Error.Code is "AUTH_018" or "AUTH_019")
            {
                await _userManager.DeleteAsync(user);
            }

            return Result.Failure<CreateSetupAdminResponse>(roleAssign.Error);
        }

        if (!await _db.NotificationSettings.AnyAsync(n => n.UserId == user.Id, cancellationToken))
        {
            await _db.NotificationSettings.AddAsync(
                NotificationSetting.CreateDefaults(user.Id),
                cancellationToken);
        }

        // 3S.2.2 — issue tokens so wizard can continue authenticated
        var issued = await _tokenService.GenerateAccessToken(user, cancellationToken);
        var ip = ResolveClientIp();
        await _db.RefreshTokens.AddAsync(
            RefreshToken.Create(
                user.Id,
                issued.HashedRefreshToken,
                ip,
                issued.RefreshTokenExpiresAt,
                userAgent: _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateSetupAdminResponse(
            UserId: user.Id.ToString(),
            Email: user.Email!,
            FullName: user.FullName,
            Role: Domain.Constants.AppRoles.SuperAdmin,
            AccessToken: issued.AccessToken,
            RefreshToken: issued.RefreshToken,
            Expiration: issued.Expiration));
    }

    private string ResolveClientIp()
    {
        var ctx = _httpContextAccessor.HttpContext;
        var forwarded = ctx?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        return ctx?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
