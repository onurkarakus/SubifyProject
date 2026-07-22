using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class AiSuggestionLogConfiguration : IEntityTypeConfiguration<AiSuggestionLog>
{
    public void Configure(EntityTypeBuilder<AiSuggestionLog> builder)
    {
        builder.ToTable("AISuggestionLogs");

        builder.Property(a => a.RequestPayload)
            .IsRequired();

        builder.Property(a => a.ResponsePayload)
            .IsRequired();

        builder.HasIndex(a => new { a.UserId, a.CreatedAt });

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
