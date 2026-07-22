using Subify.Domain.Entities;

namespace Subify.Domain.Tests;

public class UserInviteTests
{
    private static readonly Guid AdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NewUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_email_and_sets_default_expiry()
    {
        var invite = UserInvite.Create(
            "  Admin@Example.COM ",
            "hash-abc",
            AdminId,
            expiresAt: null,
            utcNow: Now);

        Assert.Equal("admin@example.com", invite.Email);
        Assert.Equal(Now.AddDays(UserInvite.DefaultExpiryDays), invite.ExpiresAt);
        Assert.True(invite.IsPending(Now));
        Assert.False(invite.IsUsed);
    }

    [Fact]
    public void TryMarkUsed_succeeds_once_and_fails_after()
    {
        var invite = UserInvite.Create("user@test.com", "hash", AdminId, utcNow: Now);

        Assert.True(invite.TryMarkUsed(NewUserId, Now.AddHours(1)));
        Assert.True(invite.IsUsed);
        Assert.Equal(NewUserId, invite.AcceptedUserId);
        Assert.False(invite.IsPending(Now.AddHours(1)));

        Assert.False(invite.TryMarkUsed(Guid.NewGuid(), Now.AddHours(2)));
    }

    [Fact]
    public void TryMarkUsed_fails_when_expired()
    {
        var invite = UserInvite.Create(
            "user@test.com",
            "hash",
            AdminId,
            expiresAt: Now.AddDays(1),
            utcNow: Now);

        Assert.False(invite.TryMarkUsed(NewUserId, Now.AddDays(2)));
        Assert.False(invite.IsUsed);
    }
}
