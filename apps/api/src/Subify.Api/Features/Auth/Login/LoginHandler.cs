using MediatR;
using Microsoft.AspNetCore.Identity;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Errors;
using Subify.Domain.Models.Auth;
using Subify.Domain.Models.Entities.Auth;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SubifyDbContext dbContext;
    private readonly ITokenService tokenService;
    private readonly IHttpContextAccessor httpContextAccessor;

    public LoginHandler(UserManager<ApplicationUser> userManager, SubifyDbContext subifyDbContext, ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
    {
        this.userManager = userManager;
        this.dbContext = subifyDbContext;
        this.tokenService = tokenService;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Result.Failure<LoginResponse>(DomainErrors.User.NotFound);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
        {
            return Result.Failure<LoginResponse>(DomainErrors.Auth.EmailNotConfirmed);
        }

        var generateTokenResult = await GenerateTokensAsync(user);

        if (!generateTokenResult.IsSuccess)
        {
            return Result.Failure<LoginResponse>(generateTokenResult.Errors);
        }

        return Result.Success(new LoginResponse
        (
            email: user.Email,
            accessToken: generateTokenResult.Value.AccessToken,
            refreshToken: generateTokenResult.Value.HashedRefreshToken,
            expiration: generateTokenResult.Value.Expiration
        ));
    }

    private async Task<Result<GenerateTokenResponse>> GenerateTokensAsync(ApplicationUser user)
    {
        var generateTokenResponse = await tokenService.GenerateTokenAsync(user);

        var http = httpContextAccessor.HttpContext;

        var forwarded = http?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
        var ipAddress = !string.IsNullOrWhiteSpace(forwarded)
            ? forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
            : http?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        var userAgent = http?.Request?.Headers.UserAgent.ToString();

        var now = DateTime.UtcNow;

        var expiresAt = now.AddDays(7);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = generateTokenResponse.HashedRefreshToken,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            RevokedReason = null,
            RevokedAt = null,
            CreatedAt = now,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ReplacedByToken = string.Empty,
            RevokedByIp = string.Empty,
            DeviceId = string.Empty,
            Id = Guid.NewGuid(),
            UpdatedAt = now
        };

        await dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        var response = new GenerateTokenResponse(generateTokenResponse.AccessToken, generateTokenResponse.RefreshToken, generateTokenResponse.HashedRefreshToken, DateTime.UtcNow.AddMinutes(15));

        return Result.Success(response);
    }
}
