using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Models.Auth;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISubifyDbContext _dbContext;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        ISubifyDbContext dbContext)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Result.Failure<LoginResponse>(DomainErrors.UserErrors.NotFound);
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
        }        

        var tokenResult = await GenerateTokenAsync(user, cancellationToken);

        if (tokenResult.IsFailure)
        {
            return Result.Failure<LoginResponse>(tokenResult.Error);
        }

        var tokenValue = tokenResult.Value;
        var response = new LoginResponse(user.Email ?? string.Empty, tokenValue.AccessToken, tokenValue.RefreshToken, tokenValue.Expiration);

        return Result.Success(response);
    }

    private async Task<Result<GenerateTokenResponse>> GenerateTokenAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        // Access JWT + plain/hash refresh pair (3.1.1 / 3.1.2)
        var generatedTokens = await _tokenService.GenerateAccessToken(user, cancellationToken);

        var httpContext = _httpContextAccessor.HttpContext;
        var forwarded = httpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
        var ipAddress = !string.IsNullOrWhiteSpace(forwarded)
            ? forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
            : httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        var userAgent = httpContext?.Request?.Headers.UserAgent.ToString();

        // Persist HASH only — plain refresh token goes to the client response only
        var refreshTokenEntity = RefreshToken.Create(
            user.Id,
            generatedTokens.HashedRefreshToken,
            ipAddress,
            generatedTokens.RefreshTokenExpiresAt,
            deviceId: null,
            userAgent: userAgent);

        await _dbContext.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);

        return Result.Success(generatedTokens);
    }
}
