using System.Text;
using Microsoft.IdentityModel.Tokens;
using Subify.Domain.Constants;

namespace Subify.Infrastructure.Authentication;

/// <summary>
/// Builds <see cref="TokenValidationParameters"/> for JWT bearer (tasks 3.1.1 / 3.1.5).
/// </summary>
public static class JwtTokenValidation
{
    public static TokenValidationParameters CreateParameters(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var secret = options.SecretKey ?? string.Empty;

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            NameClaimType = AppClaimTypes.Subject,
            RoleClaimType = AppClaimTypes.Role,
            // Task 3.1.5 — tighter than ASP.NET default (5 min)
            ClockSkew = options.ResolveClockSkew()
        };
    }
}
