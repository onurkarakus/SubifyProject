using MediatR;
using Microsoft.AspNetCore.Identity;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Auth.GetCurrentUser
{
    public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<GetCurrentUserResponse>>
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SubifyDbContext dbContext;

        public GetCurrentUserHandler(UserManager<ApplicationUser> userManager, SubifyDbContext dbContext)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
        }

        public async Task<Result<GetCurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var userInformation = await userManager.FindByIdAsync(request.UserId);
            var userProfile = await dbContext.Profiles.FindAsync(userInformation!.Id, cancellationToken);

            if (userInformation == null || userProfile == null)
            {
                return Result.Failure<GetCurrentUserResponse>(DomainErrors.User.NotFound);
            }

            return Result.Success(new GetCurrentUserResponse
            (
                Id : userInformation.Id,
                CreatedAt : userInformation.CreatedAt,
                LastLoginAt : userInformation.LastLoginAt!.Value,
                Plan : userProfile.Plan.ToString(),
                DarkTheme : userProfile.DarkTheme,
                ApplicationThemeColor : userProfile.ApplicationThemeColor,
                MonthlyBudget : userProfile.MonthlyBudget,
                UserName : userProfile.User!.UserName!,
                Email : userInformation.Email!,
                FullName : userProfile.FullName!,
                MainCurrency : userProfile.MainCurrency
            ));
        }
    }
}
