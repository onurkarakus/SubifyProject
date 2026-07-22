using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>
/// Task 2.4.2 — IUnitOfWork and ISubifyDbContext resolve to the same scoped DbContext.
/// </summary>
public class UnitOfWorkRegistrationTests
{
    [Fact]
    public void IUnitOfWork_and_ISubifyDbContext_are_same_scoped_instance()
    {
        var services = new ServiceCollection();
        services.AddDbContext<SubifyDbContext>(o =>
            o.UseSqlite("Data Source=:memory:"));
        services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SubifyDbContext>());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ISubifyDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var concrete = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();

        Assert.Same(concrete, db);
        Assert.Same(concrete, uow);
        Assert.Same(db, uow);
    }
}
