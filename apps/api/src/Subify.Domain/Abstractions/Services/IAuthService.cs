using Subify.Domain.Models.RequestEntities.Auth;
using Subify.Domain.Models.ResponseEntities.Auth;
using Subify.Domain.Models.Common;

namespace Subify.Domain.Abstractions.Services;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request);

    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);

    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
}
