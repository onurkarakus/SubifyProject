using System.Text.Json;
using System.Text.RegularExpressions;

namespace Subify.Api.Tests;

/// <summary>
/// Task 2.3.11 — Development connection string stays aligned with docker-compose defaults.
/// </summary>
public class DevConnectionStringAlignmentTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void Appsettings_Development_matches_docker_compose_defaults()
    {
        var appsettingsDev = Path.Combine(RepoRoot, "api", "Subify.Api", "appsettings.Development.json");
        var appsettings = Path.Combine(RepoRoot, "api", "Subify.Api", "appsettings.json");
        var compose = Path.Combine(RepoRoot, "docker", "docker-compose.yaml");
        var envExample = Path.Combine(RepoRoot, "docker", ".env.example");

        Assert.True(File.Exists(appsettingsDev), appsettingsDev);
        Assert.True(File.Exists(appsettings), appsettings);
        Assert.True(File.Exists(compose), compose);
        Assert.True(File.Exists(envExample), envExample);

        var devCs = ReadConnectionString(appsettingsDev);
        var baseCs = ReadConnectionString(appsettings);
        var composeText = File.ReadAllText(compose);
        var envText = File.ReadAllText(envExample);

        AssertContainsCredential(devCs, host: "localhost", database: "subify_db", user: "subify_admin", password: "SecretPassword123!");
        AssertContainsCredential(baseCs, host: "localhost", database: "subify_db", user: "subify_admin", password: "SecretPassword123!");

        Assert.Contains("subify_admin", composeText);
        Assert.Contains("SecretPassword123!", composeText);
        Assert.Contains("subify_db", composeText);
        Assert.Contains("${POSTGRES_PORT:-5432}:5432", composeText);
        Assert.Contains("${POSTGRES_USER:-subify_admin}", composeText);
        Assert.Contains("${POSTGRES_PASSWORD:-SecretPassword123!}", composeText);
        Assert.Contains("${POSTGRES_DB:-subify_db}", composeText);

        Assert.Contains("POSTGRES_USER=subify_admin", envText);
        Assert.Contains("POSTGRES_PASSWORD=SecretPassword123!", envText);
        Assert.Contains("POSTGRES_DB=subify_db", envText);
        Assert.Contains("POSTGRES_PORT=5432", envText);
    }

    private static string ReadConnectionString(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString()
            ?? string.Empty;
    }

    private static void AssertContainsCredential(
        string connectionString,
        string host,
        string database,
        string user,
        string password)
    {
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        Assert.Contains($"Host={host}", connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"Database={database}", connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"Username={user}", connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"Password={password}", connectionString, StringComparison.Ordinal);
        Assert.True(
            connectionString.Contains("Port=5432", StringComparison.OrdinalIgnoreCase)
            || !Regex.IsMatch(connectionString, @"Port=\d+", RegexOptions.IgnoreCase),
            "Port must be 5432 or omitted (Npgsql default 5432).");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docker", "docker-compose.yaml"))
                && Directory.Exists(Path.Combine(dir.FullName, "api")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root from test base directory.");
    }
}
