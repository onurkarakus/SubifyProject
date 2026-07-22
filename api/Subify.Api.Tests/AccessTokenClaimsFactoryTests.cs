using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Infrastructure.Authentication;

namespace Subify.Api.Tests;

public class AccessTokenClaimsFactoryTests
{
    [Fact]
    public void Create_includes_sub_email_jti_locale_and_roles()
    {
        var userId = Guid.CreateVersion7();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "admin@example.com",
            Locale = "EN"
        };

        var claims = AccessTokenClaimsFactory.Create(user, [AppRoles.SuperAdmin, AppRoles.User, "  Admin  "]);

        Assert.Equal(userId.ToString(), claims.Single(c => c.Type == AppClaimTypes.Subject).Value);
        Assert.Equal("admin@example.com", claims.Single(c => c.Type == AppClaimTypes.Email).Value);
        Assert.Equal("en", claims.Single(c => c.Type == AppClaimTypes.Locale).Value);

        var jti = claims.Single(c => c.Type == AppClaimTypes.JwtId).Value;
        Assert.True(Guid.TryParse(jti, out var jtiGuid));
        Assert.Equal(7, jtiGuid.Version); // UUID v7

        var roles = claims.Where(c => c.Type == AppClaimTypes.Role).Select(c => c.Value).ToArray();
        Assert.Contains(AppRoles.SuperAdmin, roles);
        Assert.Contains(AppRoles.User, roles);
        Assert.Contains("Admin", roles);
        Assert.Equal(3, roles.Length);
    }

    [Fact]
    public void Create_rejects_empty_user_id_or_email()
    {
        Assert.Throws<ArgumentException>(() =>
            AccessTokenClaimsFactory.Create(new ApplicationUser { Email = "a@b.com" }, []));

        Assert.Throws<ArgumentException>(() =>
            AccessTokenClaimsFactory.Create(
                new ApplicationUser { Id = Guid.CreateVersion7(), Email = " " },
                []));
    }

    [Fact]
    public void Written_jwt_roundtrips_required_claims()
    {
        var userId = Guid.CreateVersion7();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "user@subify.local",
            Locale = "tr"
        };

        var claims = AccessTokenClaimsFactory.Create(user, [AppRoles.User]);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForSubifyOsProjectWhichNeedsToBeLongEnough"));
        var now = DateTime.UtcNow;

        var jwt = new JwtSecurityToken(
            issuer: "SubifyOS",
            audience: "SubifyOSClient",
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(60),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var handler = new JwtSecurityTokenHandler
        {
            // Match API: keep short claim names (sub, email, jti, locale)
            MapInboundClaims = false
        };
        var tokenString = handler.WriteToken(jwt);
        var principal = handler.ValidateToken(
            tokenString,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "SubifyOS",
                ValidateAudience = true,
                ValidAudience = "SubifyOSClient",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = AppClaimTypes.Subject,
                RoleClaimType = AppClaimTypes.Role
            },
            out _);

        Assert.Equal(userId.ToString(), principal.FindFirstValue(AppClaimTypes.Subject));
        Assert.Equal("user@subify.local", principal.FindFirstValue(AppClaimTypes.Email));
        Assert.Equal("tr", principal.FindFirstValue(AppClaimTypes.Locale));
        Assert.True(principal.IsInRole(AppRoles.User));
        Assert.False(string.IsNullOrWhiteSpace(principal.FindFirstValue(AppClaimTypes.JwtId)));
    }
}
