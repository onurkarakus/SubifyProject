using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Auth.ResendConfirmation
{
    public class ResendConfirmationHandler : IRequestHandler<ResendConfirmationCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly SubifyDbContext _dbContext;

        public ResendConfirmationHandler(UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration configuration, SubifyDbContext dbContext)
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


            await _emailService.GetEmailTemplateAndSendAsync(
                emailType: Domain.Enums.EmailType.VerifyEmail,
                token: emailConfirmationToken,
                locale: (await _dbContext.Profiles.FirstOrDefaultAsync(up => up.Email == request.Email, cancellationToken))?.Locale ?? "en",
                userId: user.Id.ToString(),
                to: user?.Email,
                replacements: new Dictionary<string, string>
                {
                    { "{{FullName}}", user.FullName },
                    { "{{CurrentYear}}", DateTime.Now.Year.ToString() }
                }
            );

            return Result.Success();
        }
    }
}
