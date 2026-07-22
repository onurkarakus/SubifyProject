using Subify.Domain.Entities;

namespace Subify.Domain.Tests;

public class RefreshTokenTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void Create_stores_hash_only_fields_and_is_active()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(7);
        var token = RefreshToken.Create(UserId, "ABC123HASH", "127.0.0.1", expires, userAgent: "TestAgent");

        Assert.Equal(UserId, token.UserId);
        Assert.Equal("ABC123HASH", token.TokenHash);
        Assert.Equal("127.0.0.1", token.CreatedByIp);
        Assert.Equal(expires, token.ExpiresAt);
        Assert.True(token.IsActive());
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void MarkReplaced_sets_reason_and_replacement_hash()
    {
        var token = RefreshToken.Create(UserId, "old-hash", "1.1.1.1", DateTimeOffset.UtcNow.AddDays(1));
        token.MarkReplaced("new-hash", "2.2.2.2");

        Assert.True(token.IsRevoked);
        Assert.Equal(RefreshToken.ReasonReplaced, token.ReasonRevoked);
        Assert.Equal("new-hash", token.ReplacedByTokenHash);
        Assert.Equal("2.2.2.2", token.RevokedByIp);
        Assert.False(token.IsActive());
    }

    [Fact]
    public void Create_rejects_empty_hash()
    {
        Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(UserId, "  ", "ip", DateTimeOffset.UtcNow.AddDays(1)));
    }

    [Fact]
    public void FlagReuseAsTheft_updates_already_revoked_token()
    {
        var token = RefreshToken.Create(UserId, "hash", "1.1.1.1", DateTimeOffset.UtcNow.AddDays(1));
        token.MarkReplaced("new-hash", "1.1.1.1");

        token.FlagReuseAsTheft("9.9.9.9");

        Assert.Equal(RefreshToken.ReasonTheftDetected, token.ReasonRevoked);
        Assert.Equal("9.9.9.9", token.RevokedByIp);
        Assert.Equal("new-hash", token.ReplacedByTokenHash);
    }
}

