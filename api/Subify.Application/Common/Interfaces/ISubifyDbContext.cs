using Subify.Domain.Entities;

namespace Subify.Application.Common.Interfaces;

public interface ISubifyDbContext
{
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}