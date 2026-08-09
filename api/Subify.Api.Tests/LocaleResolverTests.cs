using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Localization;
using Subify.Domain.Constants;

namespace Subify.Domain.Tests;

// LocaleResolver lives in Application; covered here for pure header parsing + precedence.
// Note: Domain.Tests references Application only if project allows — use Application.Tests path if needed.
public class LocaleResolverTests
{
    [Theory]
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8", "tr")]
    [InlineData("en-US,en;q=0.9", "en")]
    [InlineData("de-DE,de;q=0.9,en;q=0.8", "en")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ParseAcceptLanguage(string? header, string? expected)
    {
        Assert.Equal(expected, LocaleResolver.ParseAcceptLanguage(header));
    }

    [Fact]
    public void Resolve_prefers_explicit_then_header_then_user()
    {
        var user = new FakeUser { IsAuthenticated = true, Locale = "en" };

        Assert.Equal("tr", LocaleResolver.Resolve("tr", "en", user));
        Assert.Equal("en", LocaleResolver.Resolve(null, "en-US", user));
        Assert.Equal("en", LocaleResolver.Resolve(null, null, user));
        Assert.Equal(SupportedLocales.Default, LocaleResolver.Resolve(null, null, null));
    }

    private sealed class FakeUser : ICurrentUserService
    {
        public bool IsAuthenticated { get; set; }
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string? Locale { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
        public bool IsInRole(string role) => false;
        public Guid GetRequiredUserId() => UserId ?? Guid.Empty;
    }
}
