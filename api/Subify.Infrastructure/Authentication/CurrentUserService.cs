using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;

namespace Subify.Infrastructure.Authentication;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = FindFirstValue(
                ClaimTypes.NameIdentifier,
                JwtRegisteredClaimNames.Sub,
                "sub");

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        FindFirstValue(ClaimTypes.Email, JwtRegisteredClaimNames.Email, "email");

    public string? Locale =>
        FindFirstValue(JwtRegisteredClaimNames.Locale, "locale");

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User is null)
            {
                return Array.Empty<string>();
            }

            return User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || User is null)
        {
            return false;
        }

        return User.IsInRole(role)
               || Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public Guid GetRequiredUserId()
    {
        if (!IsAuthenticated || UserId is null)
        {
            throw new UnauthorizedAccessException(DomainErrors.UserErrors.UnAuthorized.Description);
        }

        return UserId.Value;
    }

    private string? FindFirstValue(params string[] claimTypes)
    {
        if (User is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = User.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
