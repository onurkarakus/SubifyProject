using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.AI;
using Subify.Domain.Entities.Users;

namespace Subify.Infrastructure.Persistence.Configurations.AI;

public sealed class AiSuggestionLogConfiguration : IEntityTypeConfiguration<AiSuggestionLog>
{
    public void Configure(EntityTypeBuilder<AiSuggestionLog> builder)
    {
        builder.ToTable("AiSuggestionLogs");

        builder.HasKey(asl => asl.Id);

        builder.Property(asl => asl.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(asl => asl.UserId).IsRequired();
        builder.Property(asl => asl.RequestPayload).IsRequired();
        builder.Property(asl => asl.ResponsePayload);
        builder.Property(asl => asl.Model).IsRequired().HasMaxLength(50).HasDefaultValue("gpt-4o-mini");
        builder.Property(asl => asl.TokensUsed);
        builder.Property(asl => asl.ProcessingTimeMs);
        builder.Property(asl => asl.IsSuccess).HasDefaultValue(false);
        builder.Property(asl => asl.ErrorMessage).HasMaxLength(1000);
        builder.Property(asl => asl.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(asl => asl.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(asl => asl.User).WithMany(u => u.AiSuggestionLogs).HasForeignKey(asl => asl.UserId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasIndex(asl => new { asl.UserId, asl.CreatedAt }).HasDatabaseName("IX_AiSuggestionLogs_UserId_CreatedAt");
        builder.HasIndex(asl => new { asl.UserId, asl.IsSuccess, asl.CreatedAt }).HasDatabaseName("IX_AiSuggestionLogs_UserId_IsSuccess_CreatedAt");        
    }
}