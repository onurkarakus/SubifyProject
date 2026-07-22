using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Identity;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.GetSetupStatus;

public sealed record GetSetupStatusQuery : IRequest<Result<SetupStatusResponse>>;

/// <summary>Public setup status for web redirect (3S.1.2). No secrets.</summary>
public sealed record SetupStatusResponse(
    bool IsSetupComplete,
    bool HasSuperAdmin,
    bool AllowPublicRegistration,
    bool CanCreateAdmin,
    string? SuggestedNextStep,
    string? InstanceName,
    string? DefaultLocale,
    string? DefaultCurrency,
    bool HasSmtpConfigured,
    bool HasAiConfigured,
    string Version);

public sealed class GetSetupStatusHandler : IRequestHandler<GetSetupStatusQuery, Result<SetupStatusResponse>>
{
    public const string ApiVersion = "1.0.0-os";

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
        var isComplete = settings?.IsSetupComplete ?? false;
        var canCreateAdmin = !isComplete && !hasSuperAdmin;

        // Wizard path: admin → instance (defaults) → optional smtp/ai → complete
        string? next = isComplete
            ? null
            : !hasSuperAdmin
                ? "admin"
                : "instance";

        return Result.Success(new SetupStatusResponse(
            IsSetupComplete: isComplete,
            HasSuperAdmin: hasSuperAdmin,
            AllowPublicRegistration: settings?.AllowPublicRegistration ?? false,
            CanCreateAdmin: canCreateAdmin,
            SuggestedNextStep: next,
            InstanceName: settings?.InstanceName,
            DefaultLocale: settings?.DefaultLocale,
            DefaultCurrency: settings?.DefaultCurrency,
            HasSmtpConfigured: settings?.HasSmtpConfigured ?? false,
            HasAiConfigured: settings?.HasAiConfigured ?? false,
            Version: ApiVersion));
    }
}
