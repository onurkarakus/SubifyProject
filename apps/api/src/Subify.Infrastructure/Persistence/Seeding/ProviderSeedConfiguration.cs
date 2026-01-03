using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Subscriptions;
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
        builder.HasData(
             new Provider
             {
                 Id = new Guid("20000000-0000-0000-0000-000000000001"),
                 Name = "Netflix",
                 Slug = "netflix",
                 BillingCycle = "Monthly",
                 Currency = "TRY",
                 LastVerifiedAt = DateTimeOffset.UtcNow,
                 LogoUrl = "https://example.com/logos/netflix.png",
                 Price = 299.99f,
                 PriceBefore = 349.99f,
                 Region = "TR",
                 SourceUrl = "https://www.netflix.com/tr/",
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Provider
             {
                 Id = new Guid("20000000-0000-0000-0000-000000000002"),
                 Name = "Spotify",
                 Slug = "spotify",
                 BillingCycle = "Monthly",
                 Currency = "TRY",
                 LastVerifiedAt = DateTimeOffset.UtcNow,
                 LogoUrl = "https://example.com/logos/spotify.png",
                 Price = 299.99f,
                 PriceBefore = 349.99f,
                 Region = "TR",
                 SourceUrl = "https://www.spotify.com/tr/",
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             },
             new Provider
             {
                 Id = new Guid("20000000-0000-0000-0000-000000000003"),
                 Name = "Adobe Creative Cloud",
                 Slug = "adobe-creative-cloud",
                 BillingCycle = "Monthly",
                 Currency = "TRY",
                 LastVerifiedAt = DateTimeOffset.UtcNow,
                 LogoUrl = "https://example.com/logos/adobe.png",
                 Price = 499.99f,
                 PriceBefore = 599.99f,
                 Region = "TR",
                 SourceUrl = "https://www.adobe.com/tr/creativecloud.html",
                 IsActive = true,
                 CreatedAt = DateTimeOffset.UtcNow,
                 UpdatedAt = DateTimeOffset.UtcNow
             }
        );
    }
}
