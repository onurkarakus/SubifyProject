using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Models.Common;
using Subify.Domain.Models.Entities.Auth;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Models.RequestEntities.Auth;
using Subify.Domain.Models.ResponseEntities.Auth;
using Subify.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace Subify.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        SubifyDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result<RegisterResponse>.Failure("Email already in use");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Profile = new Profile { FullName = request.FullName, Email = request.Email }
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Result<RegisterResponse>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var generateTokenResult = await GenerateTokensAsync(user);

        if (!generateTokenResult.IsSuccess)
        {
            return Result<RegisterResponse>.Failure(generateTokenResult.ErrorMessage);
        }

        return Result<RegisterResponse>.Success(new RegisterResponse(user.Email!, generateTokenResult.Data.AccessToken, generateTokenResult.Data.RefreshToken, generateTokenResult.Data.Expiration));
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result<LoginResponse>.Failure("Invalid credentials");
        }

        var generateTokenResult = await GenerateTokensAsync(user);

        if (!generateTokenResult.IsSuccess)
        {
            return Result<LoginResponse>.Failure(generateTokenResult.ErrorMessage);
        }

        return Result<LoginResponse>.Success(new LoginResponse(user.Email!, generateTokenResult.Data.AccessToken, generateTokenResult.Data.RefreshToken, generateTokenResult.Data.Expiration));
    }

    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            return Result<RefreshTokenResponse>.Failure("Invalid access token");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result<RefreshTokenResponse>.Failure("User not found");
        }

        // 2. Veritabanındaki Refresh Token'ı kontrol et
        var storedRefreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.TokenHash == request.RefreshToken && x.UserId == user.Id);

        if (storedRefreshToken is null || storedRefreshToken.ExpiresAt < DateTime.UtcNow || storedRefreshToken.RevokedAt != null)
        {
            return Result<RefreshTokenResponse>.Failure("Invalid or expired refresh token");
        }

        // 3. Eski token'ı iptal et (Revoke)
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        storedRefreshToken.RevokedReason = "Token rotation";
        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.ReplacedByTokenId = storedRefreshToken.Id;

        _context.RefreshTokens.Update(storedRefreshToken);
        await _context.SaveChangesAsync();

        var generateTokenResult = await GenerateTokensAsync(user);

        if (!generateTokenResult.IsSuccess)
        {
            return Result<RefreshTokenResponse>.Failure(generateTokenResult.ErrorMessage);
        }

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(user.Email!, generateTokenResult.Data.AccessToken, generateTokenResult.Data.RefreshToken, generateTokenResult.Data.Expiration));
    }


    private async Task<Result<GenerateTokensResponse>> GenerateTokensAsync(ApplicationUser user)
    {
        var accessToken = _tokenService.CreateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var http = _httpContextAccessor.HttpContext;

        var forwarded = http?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
        var ipAddress = !string.IsNullOrWhiteSpace(forwarded)
            ? forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
            : http?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        var userAgent = http?.Request?.Headers["User-Agent"].ToString();

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

        await _context.RefreshTokens.AddAsync(refreshTokenEntity);
        await _context.SaveChangesAsync();

        var response = new GenerateTokensResponse(accessToken, tokenHash, DateTime.UtcNow.AddMinutes(15));

        return Result<GenerateTokensResponse>.Success(response);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
