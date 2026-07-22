using Microsoft.EntityFrameworkCore;
using Subify.Domain.Common;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence;

/// <summary>
/// EF Core conventions + SaveChanges helpers for UUID v7 client-side Id generation.
/// </summary>
public static class GuidIdGenerationExtensions
{
    /// <summary>
    /// Marks <see cref="BaseEntity.Id"/> as client-generated (no DB default required).
    /// </summary>
    public static void ApplyClientGuidIdConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (clr is null || clr.IsAbstract)
            {
                continue;
            }

            if (!typeof(BaseEntity).IsAssignableFrom(clr))
            {
                continue;
            }

            modelBuilder.Entity(clr)
                .Property(nameof(BaseEntity.Id))
                .ValueGeneratedNever();
        }

        // Identity user is not BaseEntity but still uses Guid PK
        modelBuilder.Entity<ApplicationUser>()
            .Property(u => u.Id)
            .ValueGeneratedOnAdd();
    }

    /// <summary>
    /// Assigns <see cref="GuidGenerator.NewId"/> to empty Ids on Added entities.
    /// </summary>
    public static void AssignGuidIdsOnAdd(this DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = GuidGenerator.NewId();
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = GuidGenerator.NewId();
            }
        }
    }
}
