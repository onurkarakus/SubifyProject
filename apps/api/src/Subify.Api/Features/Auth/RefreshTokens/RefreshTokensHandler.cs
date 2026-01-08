using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.RefreshTokens;

public class RefreshTokensHandler: IRequestHandler<RefreshTokensCommand, Result<RefreshTokensResponse>>
{
    private readonly ITokenService tokenService;
    private readonly UserManager<Domain.Models.Entities.Users.ApplicationUser> _userManager;
    private readonly Subify.Infrastructure.Persistence.SubifyDbContext _context;

    public RefreshTokensHandler(ITokenService tokenService, UserManager<Domain.Models.Entities.Users.ApplicationUser> userManager, Infrastructure.Persistence.SubifyDbContext context)
    {
        this.tokenService = tokenService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<Result<RefreshTokensResponse>> Handle(RefreshTokensCommand request, CancellationToken cancellationToken)
    {        
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
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

        // 2. Veritabanındaki Refresh Token'ı kontrol et
        var storedRefreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.TokenHash == request.RefreshToken && x.UserId == user.Id);

        if (storedRefreshToken is null || storedRefreshToken.ExpiresAt < DateTime.UtcNow || storedRefreshToken.RevokedAt != null)
        {
            return Result.Failure<RefreshTokensResponse>(DomainErrors.Auth.InvalidRefreshToken);
        }

        // 3. Eski token'ı iptal et (Revoke)
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        storedRefreshToken.RevokedReason = "Token rotation";
        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.ReplacedByTokenId = storedRefreshToken.Id;

        _context.RefreshTokens.Update(storedRefreshToken);
        await _context.SaveChangesAsync();

        var generateTokenResult = await tokenService.GenerateTokenAsync(user);

        return Result.Success(new RefreshTokensResponse(user.Email!, generateTokenResult.AccessToken, generateTokenResult.HashedRefreshToken, generateTokenResult.Expiration));
    }
}
