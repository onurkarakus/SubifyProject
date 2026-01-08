using Subify.Domain.Models.Auth;
using Subify.Domain.Models.Entities.Users;
using System.Security.Claims;

namespace Subify.Domain.Abstractions.Services;

public interface ITokenService
{
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    Task<GenerateTokenResponse> GenerateTokenAsync(ApplicationUser user);
}