using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Messaging.Inbox;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        // Primary Key is the MessageId - ensures uniqueness
        builder.HasKey(m => m.MessageId);
        builder.Property(m => m.MessageId)
            .HasColumnName("message_id")
            .ValueGeneratedNever();

        builder.Property(m => m.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Consumer)
            .HasColumnName("consumer")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.ProcessedOnUtc)
            .HasColumnName("processed_on_utc")
            .IsRequired();

        builder.Property(m => m.CorrelationId)
            .HasColumnName("correlation_id");

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id");

        // Indexes for querying
        builder.HasIndex(m => m.ProcessedOnUtc)
            .HasDatabaseName("ix_inbox_messages_processed_on_utc");

        builder.HasIndex(m => m.CorrelationId)
            .HasDatabaseName("ix_inbox_messages_correlation_id")
            .HasFilter("correlation_id IS NOT NULL");

        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("ix_inbox_messages_conversation_id")
            .HasFilter("conversation_id IS NOT NULL");
    }
}
