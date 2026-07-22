using Subify.Infrastructure.Authentication;

namespace Subify.Api.Tests;

public class RefreshTokenHasherTests
{
    [Fact]
    public void Hash_is_deterministic_sha256_hex_64_chars()
    {
        const string plain = "test-refresh-token-value";

        var h1 = RefreshTokenHasher.Hash(plain);
        var h2 = RefreshTokenHasher.Hash(plain);

        Assert.Equal(h1, h2);
        Assert.Equal(64, h1.Length);
        Assert.Matches("^[0-9A-F]{64}$", h1);
        Assert.NotEqual(plain, h1);
    }

    [Fact]
    public void GeneratePlainText_is_unique_and_hashes_differently()
    {
        var a = RefreshTokenHasher.GeneratePlainText();
        var b = RefreshTokenHasher.GeneratePlainText();

        Assert.NotEqual(a, b);
        Assert.NotEqual(RefreshTokenHasher.Hash(a), RefreshTokenHasher.Hash(b));
        Assert.False(string.IsNullOrWhiteSpace(a));
    }

    [Fact]
    public void Hash_rejects_empty()
    {
        Assert.Throws<ArgumentException>(() => RefreshTokenHasher.Hash(" "));
        Assert.Throws<ArgumentException>(() => RefreshTokenHasher.Hash(""));
    }

    [Fact]
    public void FixedTimeEquals_works_for_matching_hashes()
    {
        var hash = RefreshTokenHasher.Hash("abc");
        Assert.True(RefreshTokenHasher.FixedTimeEquals(hash, hash));
        Assert.False(RefreshTokenHasher.FixedTimeEquals(hash, RefreshTokenHasher.Hash("xyz")));
    }
}
