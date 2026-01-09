using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Auth.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly SubifyDbContext _context;

    public LogoutHandler(SubifyDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            return Result.Success();
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedReason = "Logout";
        refreshToken.IsRevoked = true;

        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}