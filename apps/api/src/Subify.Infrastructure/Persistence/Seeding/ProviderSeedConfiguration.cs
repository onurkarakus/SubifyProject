using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subify.Infrastructure.Persistence.Seeding;

internal class ProviderSeedConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        var baseTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
             new Provider
             {
                 Id = new Guid("20000000-0000-0000-0000-000000000001"),
                 Name = "Netflix",
                 Slug = "netflix",
                 LogoUrl = "https://example.com/logos/netflix.png",
                 Website = "https://www.netflix.com/tr/",
                 IsActive = true,
                 CreatedAt = baseTime,
                 UpdatedAt = baseTime
             },
             new Provider
             {
                 Id = new Guid("20000000-0000-0000-0000-000000000002"),
                 Name = "Spotify",
                 Slug = "spotify",
                 LogoUrl = "https://example.com/logos/spotify.png",
                 Website = "https://www.spotify.com/tr/",
                 IsActive = true,
                 CreatedAt = baseTime,
                 UpdatedAt = baseTime
             },
             new Provider
             {
                 Id = new Guid("20000000-0000-0000-0000-000000000003"),
                 Name = "Adobe Creative Cloud",
                 Slug = "adobe-creative-cloud",
                 LogoUrl = "https://example.com/logos/adobe.png",
                 Website = "https://www.adobe.com/tr/creativecloud.html",
                 IsActive = true,
                 CreatedAt = baseTime,
                 UpdatedAt = baseTime
             }
        );
    }
}
