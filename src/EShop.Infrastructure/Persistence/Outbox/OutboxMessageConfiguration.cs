using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Persistence.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasColumnType("jsonb") // PostgreSQL JSONB for efficient querying
            .IsRequired();

        builder.Property(m => m.OccurredOnUtc)
            .HasColumnName("occurred_on_utc")
            .IsRequired();

        builder.Property(m => m.ProcessedOnUtc)
            .HasColumnName("processed_on_utc");

        builder.Property(m => m.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        builder.Property(m => m.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        // Index for finding unprocessed messages
        builder.HasIndex(m => m.ProcessedOnUtc)
            .HasDatabaseName("ix_outbox_messages_processed_on_utc")
            .HasFilter("processed_on_utc IS NULL"); // Partial index for unprocessed only
    }
}
