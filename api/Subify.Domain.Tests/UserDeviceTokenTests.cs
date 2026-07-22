using Subify.Domain.Entities;
using Subify.Domain.Enums;

namespace Subify.Domain.Tests;

public class UserDeviceTokenTests
{
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Create_sets_active_token()
    {
        var token = UserDeviceToken.Create(UserId, " fcm-token-1 ", DevicePlatform.Android, "Pixel");

        Assert.Equal(UserId, token.UserId);
        Assert.Equal("fcm-token-1", token.Token);
        Assert.Equal(DevicePlatform.Android, token.Platform);
        Assert.Equal("Pixel", token.DeviceName);
        Assert.True(token.IsActive);
        Assert.NotNull(token.LastSeenAt);
    }

    [Fact]
    public void Deactivate_and_Activate()
    {
        var token = UserDeviceToken.Create(UserId, "t1", DevicePlatform.Ios);
        token.Deactivate();
        Assert.False(token.IsActive);

        token.Activate();
        Assert.True(token.IsActive);
    }

    [Fact]
    public void Create_rejects_unknown_platform()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserDeviceToken.Create(UserId, "t", DevicePlatform.Unknown));
    }
}
