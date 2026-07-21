using Subify.Domain.Entities;
using Subify.Domain.Models.Auth;

namespace Subify.Application.Common.Interfaces;

public interface ITokenService
{
    Task<GenerateTokenResponse> GenerateAccessToken(ApplicationUser user);    
}