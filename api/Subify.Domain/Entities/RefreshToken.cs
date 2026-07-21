using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokeAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? RevokeByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? DeviceId {get; private set;}    
    public string? UserAgent {get; private set; }
    
    public ApplicationUser User { get; private set; } = null!;

    protected RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, string createdByIp, DateTimeOffset? expiresAt, string? deviceId, string? userAgent)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedByIp = createdByIp;
        ExpiresAt = expiresAt;
        DeviceId = deviceId;
        UserAgent = userAgent;        
    }

    public void Revoke(DateTimeOffset? revokeAt, string revokedByIp, string? revokedReason, string? replacedByToken = null)
    {
        RevokeAt = DateTimeOffset.UtcNow;
        RevokeByIp = revokedByIp;
        RevokedReason = revokedReason;
        ReplacedByToken = replacedByToken;
    }
}