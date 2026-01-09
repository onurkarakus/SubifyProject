using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Auth;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.RefreshTokens;

public class RefreshTokensHandler: IRequestHandler<RefreshTokensCommand, Result<RefreshTokensResponse>>
{
    private readonly ITokenService _tokenService;
    private readonly UserManager<Domain.Models.Entities.Users.ApplicationUser> _userManager;
    private readonly Subify.Infrastructure.Persistence.SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokensHandler(
        ITokenService tokenService, 
        UserManager<Domain.Models.Entities.Users.ApplicationUser> userManager, 
        Infrastructure.Persistence.SubifyDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _tokenService = tokenService;
        _userManager = userManager;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<RefreshTokensResponse>> Handle(RefreshTokensCommand request, CancellationToken cancellationToken)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            return Result.Failure<RefreshTokensResponse>(DomainErrors.Auth.InvalidToken);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result.Failure<RefreshTokensResponse>(DomainErrors.Auth.InvalidCredentials);
        }

        var storedRefreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.TokenHash == request.RefreshToken && x.UserId == user.Id, cancellationToken);

        if (storedRefreshToken is null || storedRefreshToken.ExpiresAt < DateTime.UtcNow || storedRefreshToken.RevokedAt != null)
        {
            return Result.Failure<RefreshTokensResponse>(DomainErrors.Auth.InvalidRefreshToken);
        }

        var generateTokenResult = await _tokenService.GenerateTokenAsync(user);

        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        storedRefreshToken.RevokedReason = "Token rotation";
        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.ReplacedByToken = generateTokenResult.HashedRefreshToken;

        _context.RefreshTokens.Update(storedRefreshToken);

        var http = _httpContextAccessor.HttpContext;
        var forwarded = http?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
        var ipAddress = !string.IsNullOrWhiteSpace(forwarded)
            ? forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
            : http?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = http?.Request?.Headers.UserAgent.ToString();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = generateTokenResult.HashedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _context.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshTokensResponse(
            user.Email!,
            generateTokenResult.AccessToken,
            generateTokenResult.HashedRefreshToken,
            generateTokenResult.Expiration
        ));
    }
}
