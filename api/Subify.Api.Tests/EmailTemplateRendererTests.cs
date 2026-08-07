using Subify.Application.Common.Email;

namespace Subify.Api.Tests;

/// <summary>15.1.4 — template token replacement.</summary>
public class EmailTemplateRendererTests
{
    [Fact]
    public void Render_replaces_known_tokens()
    {
        var html = EmailTemplateRenderer.Render(
            "Hello {{FullName}}, go to {{ResetUrl}}",
            new Dictionary<string, string>
            {
                ["FullName"] = "Ada",
                ["ResetUrl"] = "https://app/reset"
            });

        Assert.Equal("Hello Ada, go to https://app/reset", html);
    }

    [Fact]
    public void Render_leaves_unknown_tokens()
    {
        var html = EmailTemplateRenderer.Render("Hi {{Missing}}", new Dictionary<string, string>());
        Assert.Equal("Hi {{Missing}}", html);
    }

    [Fact]
    public void Render_is_case_insensitive_for_token_keys()
    {
        var html = EmailTemplateRenderer.Render(
            "{{fullname}}",
            new Dictionary<string, string> { ["fullname"] = "x" });
        Assert.Equal("x", html);
    }
}
