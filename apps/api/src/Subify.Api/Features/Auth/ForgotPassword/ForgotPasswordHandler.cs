using System.Web;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly SubifyDbContext _dbContext;

    public ForgotPasswordHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        SubifyDbContext dbContext)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null) return Result.Success();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = HttpUtility.UrlEncode(token);

        var frontendUrl = _configuration["AppUrl"] ?? "http://localhost:3000";
        var resetLink = $"{frontendUrl}/reset-password?email={request.Email}&token={encodedToken}";

        var template = await _dbContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == "ForgotPassword" && t.LanguageCode == "tr-TR", cancellationToken);

        string subject = template?.Subject ?? "Şifre Sıfırlama";
        string body = template?.Body.Replace("{{ResetLink}}", resetLink)
                      ?? $"Şifrenizi sıfırlamak için tıklayın: {resetLink}";

        await _emailService.SendEmailAsync(user.Email!, subject, body);

        return Result.Success();
    }
}