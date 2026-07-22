using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Common;
using Subify.Domain.Constants;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds Identity roles: SuperAdmin, Admin, User (task 2.3.4).
/// Idempotent — skips roles that already exist (task 2.3.10).
/// </summary>
public sealed class RolesDataSeeder : IDataSeeder
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<RolesDataSeeder> _logger;

    public RolesDataSeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<RolesDataSeeder> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public int Order => 10;

    public string Name => "Roles";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in AppRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogDebug("Role {Role} already exists; skipping.", roleName);
                continue;
            }

            var role = new IdentityRole<Guid>(roleName)
            {
                // UUID v7 — IdentityRole is not BaseEntity; set Id explicitly
                Id = GuidGenerator.NewId()
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException(
                    $"Failed to seed Identity role '{roleName}': {errors}");
            }

            _logger.LogInformation("Seeded Identity role {Role}.", roleName);
        }
    }
}
