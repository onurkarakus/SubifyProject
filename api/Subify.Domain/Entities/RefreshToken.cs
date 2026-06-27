using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset Expiresat { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;
    public DateTimeOffset? RevokeAt { get; private set; }
    public string? RevokeByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? ReasonRevoked { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    protected RefreshToken() { }

    public RefreshToken(Guid userId, string token, DateTimeOffset expiresAt, string createdByIp)
    {
        UserId = userId;
        Token = token;
        Expiresat = expiresAt;
        CreatedByIp = createdByIp;
    }

    public void Revoke(string ipAddress, string reason, string? replacedByToken = null)
    {
        RevokeAt = DateTimeOffset.UtcNow;
        RevokeByIp = ipAddress;
        ReasonRevoked = reason;
        ReplacedByToken = replacedByToken;
    }
}