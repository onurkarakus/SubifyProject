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

        await _emailService.GetEmailTemplateAndSendAsync(
            emailType: Domain.Enums.EmailType.ForgotPassword,
            token: token,
            locale: (await _dbContext.Profiles.FirstOrDefaultAsync(up => up.Email == request.Email, cancellationToken))?.Locale ?? "en-US",
            userId: user.Id.ToString(),
            to: user?.Email,
            replacements: new Dictionary<string, string>
            {
                { "USER_NAME", user?.UserName ?? "User" }
            }
        );

        return Result.Success();
    }
}