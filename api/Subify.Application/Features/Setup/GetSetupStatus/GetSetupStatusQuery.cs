using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Identity;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.GetSetupStatus;

public sealed record GetSetupStatusQuery : IRequest<Result<SetupStatusResponse>>;

public sealed record SetupStatusResponse(
    bool IsSetupComplete,
    bool HasSuperAdmin,
    bool AllowPublicRegistration,
    string? InstanceName);

public sealed class GetSetupStatusHandler : IRequestHandler<GetSetupStatusQuery, Result<SetupStatusResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetSetupStatusHandler(ISubifyDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<Result<SetupStatusResponse>> Handle(
        GetSetupStatusQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var hasSuperAdmin = await SuperAdminBootstrap.HasAnySuperAdminAsync(_userManager);

        return Result.Success(new SetupStatusResponse(
            IsSetupComplete: settings?.IsSetupComplete ?? false,
            HasSuperAdmin: hasSuperAdmin,
            AllowPublicRegistration: settings?.AllowPublicRegistration ?? false,
            InstanceName: settings?.InstanceName));
    }
}
