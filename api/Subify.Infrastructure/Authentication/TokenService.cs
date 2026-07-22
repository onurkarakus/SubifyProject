using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Models.Auth;

namespace Subify.Infrastructure.Authentication;

/// <summary>
/// JWT access + refresh token generation (tasks 3.1.1 / 3.1.2).
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IOptions<JwtOptions> jwtOptions, UserManager<ApplicationUser> userManager)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<GenerateTokenResponse> GenerateAccessToken(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        EnsureJwtOptionsValid();

        var roles = await _userManager.GetRolesAsync(user);
        cancellationToken.ThrowIfCancellationRequested();

        var claims = AccessTokenClaimsFactory.Create(user, roles);

        var now = DateTime.UtcNow;
        var accessMinutes = AccessTokenLifetimeMinutes;
        var accessExpiration = now.AddMinutes(accessMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpiration,
            signingCredentials: creds);

        token.Payload[JwtRegisteredClaimNames.Iat] = EpochTime.GetIntDate(now);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = CreateRefreshTokenMaterial(DateTimeOffset.UtcNow);

        return new GenerateTokenResponse(
            AccessToken: accessToken,
            RefreshToken: refresh.PlainText,
            HashedRefreshToken: refresh.TokenHash,
            Expiration: accessExpiration,
            RefreshTokenExpiresAt: refresh.ExpiresAt);
    }

    /// <inheritdoc />
    public RefreshTokenMaterial CreateRefreshTokenMaterial(DateTimeOffset? utcNow = null)
    {
        var now = utcNow ?? DateTimeOffset.UtcNow;
        var days = RefreshTokenLifetimeDays;
        var plain = RefreshTokenHasher.GeneratePlainText();
        var hash = RefreshTokenHasher.Hash(plain);

        return new RefreshTokenMaterial(plain, hash, now.AddDays(days));
    }

    /// <inheritdoc />
    public string HashRefreshToken(string plainRefreshToken) =>
        RefreshTokenHasher.Hash(plainRefreshToken);

    private int AccessTokenLifetimeMinutes => _jwtOptions.ResolveAccessTokenLifetime();

    private int RefreshTokenLifetimeDays => _jwtOptions.ResolveRefreshTokenDays();

    private void EnsureJwtOptionsValid()
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.SecretKey) || _jwtOptions.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JwtOptions.SecretKey must be configured and at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
        {
            throw new InvalidOperationException("JwtOptions.Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
        {
            throw new InvalidOperationException("JwtOptions.Audience must be configured.");
        }
    }
}

