using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>
/// Task 2.4.1 — concrete context implements the full application DbSet surface.
/// </summary>
public class ISubifyDbContextContractTests
{
    [Fact]
    public void SubifyDbContext_implements_ISubifyDbContext()
    {
        Assert.True(typeof(ISubifyDbContext).IsAssignableFrom(typeof(SubifyDbContext)));
    }

    [Theory]
    [InlineData(nameof(ISubifyDbContext.Users), typeof(ApplicationUser))]
    [InlineData(nameof(ISubifyDbContext.RefreshTokens), typeof(RefreshToken))]
    [InlineData(nameof(ISubifyDbContext.UserInvites), typeof(UserInvite))]
    [InlineData(nameof(ISubifyDbContext.UserDeviceTokens), typeof(UserDeviceToken))]
    [InlineData(nameof(ISubifyDbContext.Categories), typeof(Category))]
    [InlineData(nameof(ISubifyDbContext.UserCategories), typeof(UserCategory))]
    [InlineData(nameof(ISubifyDbContext.Providers), typeof(Provider))]
    [InlineData(nameof(ISubifyDbContext.Subscriptions), typeof(Subscription))]
    [InlineData(nameof(ISubifyDbContext.SubscriptionPriceHistories), typeof(SubscriptionPriceHistory))]
    [InlineData(nameof(ISubifyDbContext.SystemSettings), typeof(SystemSettings))]
    [InlineData(nameof(ISubifyDbContext.Resources), typeof(Resource))]
    [InlineData(nameof(ISubifyDbContext.NotificationSettings), typeof(NotificationSetting))]
    [InlineData(nameof(ISubifyDbContext.EmailTemplates), typeof(EmailTemplates))]
    [InlineData(nameof(ISubifyDbContext.EmailSendLogs), typeof(EmailSendLog))]
    [InlineData(nameof(ISubifyDbContext.ActivityLogs), typeof(ActivityLog))]
    [InlineData(nameof(ISubifyDbContext.AISuggestionLogs), typeof(AiSuggestionLog))]
    [InlineData(nameof(ISubifyDbContext.ExchangeRateSnapshots), typeof(ExchangeRateSnapshot))]
    public void Interface_exposes_DbSet_for_entity(string propertyName, Type entityType)
    {
        var property = typeof(ISubifyDbContext).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);

        var expected = typeof(DbSet<>).MakeGenericType(entityType);
        Assert.Equal(expected, property!.PropertyType);

        // Identity base may declare Users; use GetProperties to avoid AmbiguousMatchException
        var concrete = typeof(SubifyDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name == propertyName && p.PropertyType == expected);
        Assert.NotNull(concrete);
    }

    [Fact]
    public void Interface_exposes_SaveChangesAsync_via_IUnitOfWork()
    {
        Assert.True(typeof(IUnitOfWork).IsAssignableFrom(typeof(ISubifyDbContext)));
        Assert.True(typeof(IUnitOfWork).IsAssignableFrom(typeof(SubifyDbContext)));

        var method = typeof(IUnitOfWork).GetMethod(
            nameof(IUnitOfWork.SaveChangesAsync),
            [typeof(CancellationToken)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<int>), method!.ReturnType);
    }
}

