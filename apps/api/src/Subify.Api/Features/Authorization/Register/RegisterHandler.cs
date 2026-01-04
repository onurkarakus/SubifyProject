using MediatR;
using Microsoft.AspNetCore.Identity;
using Subify.Api.Common.Extensions;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Auth;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Models.RequestEntities.Auth;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace Subify.Api.Features.Authorization.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SubifyDbContext dbContext;
    private readonly ITokenService tokenService;
    private readonly IHttpContextAccessor httpContextAccessor;

    public RegisterHandler(UserManager<ApplicationUser> userManager, 
        SubifyDbContext dbContext, 
        ITokenService tokenService, 
        IHttpContextAccessor httpContextAccessor)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.tokenService = tokenService;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result.Failure<RegisterResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Profile = new Profile { FullName = request.FullName, Email = request.Email }
        };

        var emailValidationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var createResult = await userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return Result.Failure<RegisterResponse>(createResult.GetErrors());
        }

        var generateTokenResult = await GenerateTokensAsync(user);

        if (!generateTokenResult.IsSuccess)
        {
            return Result.Failure<RegisterResponse>(generateTokenResult.Error);
        }

        return Result<RegisterResponse>.Success(new RegisterResponse(
            user.Email!, 
            generateTokenResult.Value.AccessToken, 
            generateTokenResult.Value.RefreshToken, 
            generateTokenResult.Value.Expiration));
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
