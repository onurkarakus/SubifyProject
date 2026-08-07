using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Profile.GetProfile;

/// <summary>Get authenticated user's profile (5.3.1).</summary>
public sealed record GetProfileQuery : IRequest<Result<ProfileResponse>>;

public sealed class GetProfileHandler : IRequestHandler<GetProfileQuery, Result<ProfileResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public GetProfileHandler(UserManager<ApplicationUser> userManager, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<Result<ProfileResponse>> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ProfileResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user is null)
        {
            return Result.Failure<ProfileResponse>(DomainErrors.ProfileErrors.ProfileNotFound);
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Result.Success(new ProfileResponse(
            Id: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: user.FullName,
            Locale: user.Locale,
            MainCurrency: user.MainCurrency,
            MonthlyBudget: user.MonthlyBudget,
            ApplicationThemeColor: user.ApplicationThemeColor,
            DarkTheme: user.DarkTheme,
            Roles: roles.ToArray(),
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt));
    }
}
