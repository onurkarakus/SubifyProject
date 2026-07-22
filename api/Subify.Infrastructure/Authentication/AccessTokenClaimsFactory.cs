using System.Security.Claims;
using Subify.Domain.Common;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Authentication;

/// <summary>
/// Builds access-token claims for Subify OS (task 3.1.1):
/// <c>sub</c>, <c>email</c>, <c>jti</c>, <c>locale</c>, role claims.
/// </summary>
public static class AccessTokenClaimsFactory
{
    public static IReadOnlyList<Claim> Create(ApplicationUser user, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        if (user.Id == Guid.Empty)
        {
            throw new ArgumentException("User Id is required for access token.", nameof(user));
        }

        var email = user.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("User email is required for access token.", nameof(user));
        }

        var locale = SupportedLocales.Normalize(user.Locale);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.Subject, user.Id.ToString()),
            new(AppClaimTypes.Email, email),
            new(AppClaimTypes.JwtId, GuidGenerator.NewId().ToString()),
            new(AppClaimTypes.Locale, locale)
        };

        foreach (var role in roles
                     .Where(r => !string.IsNullOrWhiteSpace(r))
                     .Select(r => r.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(AppClaimTypes.Role, role));
        }

        return claims;
    }
}
