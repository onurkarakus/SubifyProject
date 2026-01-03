using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Subscriptions;

namespace Subify.Infrastructure.Persistence.Seeding;

internal class CategorySeedConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasData(
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000001"),
                 Slug = "streaming",
                 Icon = "play-circle",
                 Color = "#E50914",
                 SortOrder = 1,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000002"),
                 Slug = "music",
                 Icon = "music",
                 Color = "#1DB954",
                 SortOrder = 2,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000003"),
                 Slug = "productivity",
                 Icon = "briefcase",
                 Color = "#0078D4",
                 SortOrder = 3,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000004"),
                 Slug = "gaming",
                 Icon = "gamepad-2",
                 Color = "#9147FF",
                 SortOrder = 4,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000005"),
                 Slug = "cloud-storage",
                 Icon = "cloud",
                 Color = "#0066FF",
                 SortOrder = 5,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000006"),
                 Slug = "news-magazines",
                 Icon = "newspaper",
                 Color = "#FF6B00",
                 SortOrder = 6,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000007"),
                 Slug = "fitness-health",
                 Icon = "heart-pulse",
                 Color = "#FF2D55",
                 SortOrder = 7,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000008"),
                 Slug = "education",
                 Icon = "graduation-cap",
                 Color = "#5856D6",
                 SortOrder = 8,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-000000000009"),
                 Slug = "utilities",
                 Icon = "wrench",
                 Color = "#8E8E93",
                 SortOrder = 9,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Category
             {
                 Id = new Guid("10000000-0000-0000-0000-00000000000A"),
                 Slug = "other",
                 Icon = "folder",
                 Color = "#636366",
                 SortOrder = 99,
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             }
         );
    }
}
