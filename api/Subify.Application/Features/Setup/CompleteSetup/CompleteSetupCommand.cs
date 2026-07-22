using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Identity;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.CompleteSetup;

/// <summary>Finish wizard: requires SuperAdmin; sets IsSetupComplete (3S.7.1).</summary>
public sealed record CompleteSetupCommand : IRequest<Result<CompleteSetupResponse>>;

public sealed record CompleteSetupResponse(
    bool IsSetupComplete,
    DateTimeOffset SetupCompletedAt,
    string InstanceName);

public sealed class CompleteSetupHandler : IRequestHandler<CompleteSetupCommand, Result<CompleteSetupResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public CompleteSetupHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<Result<CompleteSetupResponse>> Handle(
        CompleteSetupCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<CompleteSetupResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return Result.Failure<CompleteSetupResponse>(DomainErrors.Setup.SettingsNotInitialized);
        }

        if (settings.IsSetupComplete)
        {
            return Result.Failure<CompleteSetupResponse>(DomainErrors.Setup.AlreadyComplete);
        }

        if (!await SuperAdminBootstrap.HasAnySuperAdminAsync(_userManager))
        {
            return Result.Failure<CompleteSetupResponse>(DomainErrors.Setup.SuperAdminRequired);
        }

        settings.MarkSetupComplete();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CompleteSetupResponse(
            IsSetupComplete: true,
            SetupCompletedAt: settings.SetupCompletedAt!.Value,
            InstanceName: settings.InstanceName ?? "Subify"));
    }
}
