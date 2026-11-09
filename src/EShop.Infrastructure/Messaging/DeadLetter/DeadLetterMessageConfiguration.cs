using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Messaging.DeadLetter;

public class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("dead_letter_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.OriginalMessageId)
            .HasColumnName("original_message_id")
            .IsRequired();

        builder.Property(m => m.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(m => m.SourceQueue)
            .HasColumnName("source_queue")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.FailedConsumer)
            .HasColumnName("failed_consumer")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.Property(m => m.StackTrace)
            .HasColumnName("stack_trace");

        builder.Property(m => m.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(m => m.FailedAtUtc)
            .HasColumnName("failed_at_utc")
            .IsRequired();

        builder.Property(m => m.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(m => m.LastRetryAtUtc)
            .HasColumnName("last_retry_at_utc");

        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.CorrelationId)
            .HasColumnName("correlation_id");

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id");

        builder.Property(m => m.Notes)
            .HasColumnName("notes");

        // Indexes for efficient querying
        builder.HasIndex(m => m.Status)
            .HasDatabaseName("ix_dead_letter_messages_status");

        builder.HasIndex(m => m.FailedAtUtc)
            .HasDatabaseName("ix_dead_letter_messages_failed_at_utc");

        builder.HasIndex(m => m.MessageType)
            .HasDatabaseName("ix_dead_letter_messages_message_type");

        builder.HasIndex(m => m.SourceQueue)
            .HasDatabaseName("ix_dead_letter_messages_source_queue");

        builder.HasIndex(m => m.CorrelationId)
            .HasDatabaseName("ix_dead_letter_messages_correlation_id")
            .HasFilter("correlation_id IS NOT NULL");

        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("ix_dead_letter_messages_conversation_id")
            .HasFilter("conversation_id IS NOT NULL");
    }
}
