using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.RequestEntities;


namespace Subify.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<IActionResult> LoginTest([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Geçersiz e-posta ve şifre" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateAccessToken(user, roles);

        return Ok(new { AccessToken = token });
    }
}