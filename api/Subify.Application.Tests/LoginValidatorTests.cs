using FluentValidation.TestHelper;
using Subify.Application.Features.Auth.Login;

namespace Subify.Application.Tests;

/// <summary>12.1.4 — Login FluentValidation.</summary>
public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Valid_login_passes()
    {
        var result = _validator.TestValidate(new LoginCommand("a@b.com", "Password1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Email_must_be_valid(string email)
    {
        var result = _validator.TestValidate(new LoginCommand(email, "Password1"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Password_required()
    {
        var result = _validator.TestValidate(new LoginCommand("a@b.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
