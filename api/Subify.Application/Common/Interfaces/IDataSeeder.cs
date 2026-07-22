namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Idempotent startup data seeder (task 2.3.3).
/// Implementations must be safe to run on every application start (task 2.3.10).
/// Register via DI; <c>DatabaseSeeder</c> runs all seeders ordered by <see cref="Order"/>.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Execution order (lower first). Suggested ranges:
    /// 10 roles · 20 categories · 30 providers · 40 resources · 50 system settings · 60 email templates.
    /// </summary>
    int Order { get; }

    /// <summary>Stable name for logs (e.g. <c>Roles</c>, <c>Categories</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Seeds missing baseline data only (task 2.3.10).
    /// Must be safe on every application start: no duplicate rows, no throw when data already exists,
    /// and must not overwrite admin/custom changes to existing rows.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}

