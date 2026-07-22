using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Infrastructure.Persistence.Seeding;

namespace Subify.Api.Tests;

public class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_runs_seeders_ordered_by_Order_then_Name()
    {
        var executed = new List<string>();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddScoped<IDataSeeder>(_ => new RecordingSeeder(20, "Bravo", executed));
        services.AddScoped<IDataSeeder>(_ => new RecordingSeeder(10, "Alpha", executed));
        services.AddScoped<IDataSeeder>(_ => new RecordingSeeder(10, "Zulu", executed));

        await using var provider = services.BuildServiceProvider();
        await DatabaseSeeder.SeedAsync(provider);

        Assert.Equal(new[] { "Alpha", "Zulu", "Bravo" }, executed);
    }

    [Fact]
    public async Task SeedAsync_with_no_seeders_completes()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());

        await using var provider = services.BuildServiceProvider();
        await DatabaseSeeder.SeedAsync(provider);
    }

    private sealed class RecordingSeeder : IDataSeeder
    {
        private readonly List<string> _executed;

        public RecordingSeeder(int order, string name, List<string> executed)
        {
            Order = order;
            Name = name;
            _executed = executed;
        }

        public int Order { get; }
        public string Name { get; }

        public Task SeedAsync(CancellationToken cancellationToken = default)
        {
            _executed.Add(Name);
            return Task.CompletedTask;
        }
    }
}
