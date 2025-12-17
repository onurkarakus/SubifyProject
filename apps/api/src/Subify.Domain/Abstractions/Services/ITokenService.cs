using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Models.ResponseEntities.Auth;
using System.Security.Claims;

namespace Subify.Domain.Abstractions.Services;

public interface ITokenService
{
    string CreateToken(ApplicationUser user);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}