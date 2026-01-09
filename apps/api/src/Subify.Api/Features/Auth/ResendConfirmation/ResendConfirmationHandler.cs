using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using Subify.Infrastructure.Services;

namespace Subify.Api.Features.Auth.ResendConfirmation
{
    public class ResendConfirmationHandler : IRequestHandler<ResendConfirmationCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly SubifyDbContext _dbContext;

        public ResendConfirmationHandler(UserManager<ApplicationUser> userManager, EmailService emailService, IConfiguration configuration, SubifyDbContext dbContext)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<Result> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Result.Failure(Domain.Errors.DomainErrors.User.NotFound);
            }

            if (user.EmailConfirmed)
            {
                return Result.Failure(Domain.Errors.DomainErrors.Auth.EmailAlreadyConfirmed);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = System.Web.HttpUtility.UrlEncode(emailConfirmationToken);
            var userId = user.Id.ToString();
            var frontendUrl =  _configuration["AppUrl"] ?? "http://localhost:3000";
            var verificationLink = $"{frontendUrl}/verify-email?userId={userId}&token={encodedToken}";

            var emailTemplate = await _dbContext.EmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                t.LanguageCode == request.Locale && t.Name == "VerifyEmail", cancellationToken);

            string subject;
            string body;

            if (emailTemplate != null)
            {
                subject = emailTemplate.Subject;

                body = emailTemplate.Body
                    .Replace("{{FullName}}", user.FullName)
                    .Replace("{{VerifyLink}}", verificationLink)
                    .Replace("{{CurrentYear}}", DateTime.Now.Year.ToString());
            }

            else
            {
                subject = "Subify - E-posta Doğrulama";
                body = $"Merhaba {user.FullName}, <br> Hesabınızı doğrulamak için <a href='{verificationLink}'>tıklayın</a>.";
            }

            await _emailService.SendEmailAsync(user.Email!, subject, body);

            return Result.Success();
        }
    }
}
