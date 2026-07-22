using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Abstractions.Common;

namespace Subify.Infrastructure.Persistence;

/// <summary>
/// Applies a global EF query filter so soft-deleted rows (<see cref="ISoftDeletable.DeletedAt"/> != null)
/// are excluded by default. Use <c>IgnoreQueryFilters()</c> when admin/history needs deleted rows.
/// </summary>
public static class SoftDeleteQueryExtensions
{
    private static readonly MethodInfo SetFilterMethod = typeof(SoftDeleteQueryExtensions)
        .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType is null || clrType.IsAbstract)
            {
                continue;
            }

            if (!typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                continue;
            }

            SetFilterMethod
                .MakeGenericMethod(clrType)
                .Invoke(null, [modelBuilder]);
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        // DeletedAt == null means "not soft-deleted"
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
