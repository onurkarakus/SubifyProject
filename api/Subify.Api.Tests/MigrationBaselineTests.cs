using System.Text.RegularExpressions;

namespace Subify.Api.Tests;

/// <summary>
/// Task 2.3.12 — migration baseline is complete and ordered; no orphan designers.
/// </summary>
public class MigrationBaselineTests
{
    private static readonly string MigrationsDir = FindMigrationsDir();

    [Fact]
    public void Migration_chain_has_expected_count_and_tip()
    {
        var ids = GetMigrationIds();

        Assert.True(ids.Count >= 12, $"Expected at least 12 migrations, got {ids.Count}");
        Assert.Equal("20260716202334_InitialCreate", ids[0]);
        Assert.Contains(ids, id => id.EndsWith("_NotificationSettingsEmailDefaultFalse", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_migration_has_designer_and_readme()
    {
        var ids = GetMigrationIds();

        foreach (var id in ids)
        {
            var cs = Path.Combine(MigrationsDir, $"{id}.cs");
            var designer = Path.Combine(MigrationsDir, $"{id}.Designer.cs");
            Assert.True(File.Exists(cs), $"Missing {cs}");
            Assert.True(File.Exists(designer), $"Missing designer for {id}");
        }

        Assert.True(File.Exists(Path.Combine(MigrationsDir, "SubifyDbContextModelSnapshot.cs")));
        Assert.True(File.Exists(Path.Combine(MigrationsDir, "README.md")));
    }

    [Fact]
    public void Migration_ids_are_strictly_increasing_timestamps()
    {
        var ids = GetMigrationIds();
        var timestamps = ids.Select(id => id[..14]).ToList();

        for (var i = 1; i < timestamps.Count; i++)
        {
            Assert.True(
                string.CompareOrdinal(timestamps[i - 1], timestamps[i]) < 0,
                $"Migration order broken: {ids[i - 1]} then {ids[i]}");
        }
    }

    [Fact]
    public void Snapshot_targets_SubifyDbContext()
    {
        var snapshot = File.ReadAllText(Path.Combine(MigrationsDir, "SubifyDbContextModelSnapshot.cs"));
        Assert.Contains("typeof(SubifyDbContext)", snapshot);
        Assert.Contains("CompleteEntityTypeConfigurations", File.ReadAllText(
            Path.Combine(MigrationsDir, "20260722101332_CompleteEntityTypeConfigurations.Designer.cs")));
    }

    [Fact]
    public void Rename_migrations_use_RenameColumn_not_drop_add_for_locale_and_logo()
    {
        // Data-preserving renames from OS cleanup (2.1.x)
        var locale = File.ReadAllText(Path.Combine(MigrationsDir, "20260722094204_RenameLocateToLocale.cs"));
        var logo = File.ReadAllText(Path.Combine(MigrationsDir, "20260722095705_RenameProviderLogoutToLogoUrl.cs"));

        Assert.Contains("RenameColumn", locale);
        Assert.DoesNotContain("DropColumn", locale);
        Assert.Contains("RenameColumn", logo);
        Assert.DoesNotContain("DropColumn", logo);
    }

    private static IReadOnlyList<string> GetMigrationIds()
    {
        var files = Directory.GetFiles(MigrationsDir, "*.cs")
            .Select(Path.GetFileName)
            .Where(n => n is not null
                        && !n.EndsWith(".Designer.cs", StringComparison.Ordinal)
                        && n != "SubifyDbContextModelSnapshot.cs"
                        && n != "README.md")
            .Select(n => n![..^3]) // strip .cs
            .Where(n => Regex.IsMatch(n, @"^\d{14}_"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        return files;
    }

    private static string FindMigrationsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "api", "Subify.Infrastructure", "Migrations");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            // When running from Infrastructure bin directly
            candidate = Path.Combine(dir.FullName, "Migrations");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "SubifyDbContextModelSnapshot.cs")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Migrations folder.");
    }
}
