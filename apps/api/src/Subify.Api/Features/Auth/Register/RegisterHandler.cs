using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Api.Common.Extensions;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Auth;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Models.RequestEntities.Auth;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace Subify.Api.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SubifyDbContext dbContext;
    private readonly ITokenService tokenService;
    private readonly IEmailService emailService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IConfiguration configuration;

    public RegisterHandler(UserManager<ApplicationUser> userManager,
        SubifyDbContext dbContext,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IConfiguration configuration)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.tokenService = tokenService;
        this.httpContextAccessor = httpContextAccessor;
        this.emailService = emailService;
        this.configuration = configuration;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result.Failure<RegisterResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                EmailConfirmed = false
            };

            var createResult = await userManager.CreateAsync(newUser, request.Password);

            if (!createResult.Succeeded)
            {
                return Result.Failure<RegisterResponse>(createResult.GetErrors());
            }

            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                FullName = request.FullName,
                MainCurrency = request.MainCurrency,
                Locale = request.Locale,
                CreatedAt = DateTimeOffset.UtcNow,
                Email = request.Email,
                DarkTheme = request.UseDarkTheme,
                MonthlyBudget = request.MonthlyBudget,
                ApplicationThemeColor = request.ApplicationThemeColor,
                Plan = PlanType.Free,
                NotificationSettings = new NotificationSetting
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    DaysBeforeRenewal = request.DaysBeforeRenewal,
                    EmailEnabled = request.NotificationEmailEnabled,
                    PushEnabled = request.NotificationPushEnabled,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            };

            await dbContext.Profiles.AddAsync(profile, cancellationToken);

            var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(newUser);
            var encodedToken = System.Web.HttpUtility.UrlEncode(emailConfirmationToken);
            var userId = newUser.Id.ToString();
            var frontendUrl = configuration["AppUrl"] ?? "http://localhost:3000";
            var verificationLink = $"{frontendUrl}/verify-email?userId={userId}&token={encodedToken}";

            var emailTemplate = await dbContext.EmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t=>
                t.LanguageCode == request.Locale && t.Name == "VerifyEmail", cancellationToken);

            string subject;
            string body;

            if (emailTemplate != null)
            {
                subject = emailTemplate.Subject;

                body = emailTemplate.Body
                    .Replace("{{FullName}}", request.FullName)
                    .Replace("{{VerifyLink}}", verificationLink)
                    .Replace("{{CurrentYear}}", DateTime.Now.Year.ToString());
            }

            else
            {
                subject = "Subify - E-posta Doğrulama";
                body = $"Merhaba {request.FullName}, <br> Hesabınızı doğrulamak için <a href='{verificationLink}'>tıklayın</a>.";
            }

            await emailService.SendEmailAsync(newUser.Email!, subject, body);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new RegisterResponse
            (
                Email: newUser.Email!,
                UserId: newUser.Id.ToString(),
                Expiration: DateTime.UtcNow.AddMinutes(15),
                Message: "Kayıt başarılı! Lütfen e-posta adresinizi doğrulayın."
            ));
        }
        catch (Exception)
        {

            throw;
        }
    }

    private async Task<Result<GenerateTokensResponse>> GenerateTokensAsync(ApplicationUser user)
    {
        var accessToken = tokenService.CreateToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        var http = httpContextAccessor.HttpContext;

        var forwarded = http?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
        var ipAddress = !string.IsNullOrWhiteSpace(forwarded)
            ? forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
            : http?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        var userAgent = http?.Request?.Headers.UserAgent.ToString();

        var tokenHash = HashToken(refreshToken);

        var now = DateTime.UtcNow;

        var expiresAt = now.AddDays(7);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            RevokedReason = null,
            RevokedAt = null,
            CreatedAt = now,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        var response = new GenerateTokensResponse(accessToken, tokenHash, DateTime.UtcNow.AddMinutes(15));

        return Result.Success(response);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
