using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Api.Common.Extensions;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SubifyDbContext dbContext;
    private readonly IEmailService emailService;
    private readonly IConfiguration configuration;

    public RegisterHandler(UserManager<ApplicationUser> userManager,
        SubifyDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.emailService = emailService;
        this.configuration = configuration;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result.Failure<RegisterResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                EmailConfirmed = false
            };

            var createResult = await userManager.CreateAsync(newUser, request.Password);

            if (!createResult.Succeeded)
            {
                return Result.Failure<RegisterResponse>(createResult.GetErrors());
            }

            var profile = new Profile
            {
                Id = newUser.Id,
                UserId = newUser.Id,
                FullName = request.FullName,
                MainCurrency = request.MainCurrency,
                Locale = request.Locale,
                CreatedAt = DateTimeOffset.UtcNow,
                Email = request.Email,
                DarkTheme = request.UseDarkTheme,
                MonthlyBudget = request.MonthlyBudget,
                ApplicationThemeColor = request.ApplicationThemeColor,
                Plan = PlanType.Free,
                NotificationSettings = new NotificationSetting
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    DaysBeforeRenewal = request.DaysBeforeRenewal,
                    EmailEnabled = request.NotificationEmailEnabled,
                    PushEnabled = request.NotificationPushEnabled,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            };

            await dbContext.Profiles.AddAsync(profile, cancellationToken);

            var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(newUser);            
            
            await emailService.GetEmailTemplateAndSendAsync(EmailType.VerifyEmail, emailConfirmationToken, request.Locale, newUser.Id.ToString(), newUser.Email!, new Dictionary<string, string>
            {
                { "{{FullName}}", request.FullName },
                { "{{CurrentYear}}", DateTime.Now.Year.ToString() }
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new RegisterResponse
            (
                Email: newUser.Email!,
                UserId: newUser.Id.ToString(),
                Expiration: DateTime.UtcNow.AddMinutes(15),
                Message: "Kayıt başarılı! Lütfen e-posta adresinizi doğrulayın."
            ));
        }
        catch (Exception)
        {

            throw;
        }
    }

    
}
