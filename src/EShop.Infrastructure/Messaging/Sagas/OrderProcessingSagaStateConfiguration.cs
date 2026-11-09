using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Messaging.Sagas;

public class OrderProcessingSagaStateConfiguration : IEntityTypeConfiguration<OrderProcessingSagaState>
{
    public void Configure(EntityTypeBuilder<OrderProcessingSagaState> builder)
    {
        builder.ToTable("order_processing_sagas");

        builder.HasKey(x => x.CorrelationId);
        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .ValueGeneratedNever();

        builder.Property(x => x.CurrentState)
            .HasColumnName("current_state")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.PaymentId)
            .HasColumnName("payment_id");

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.StockReservedAtUtc)
            .HasColumnName("stock_reserved_at_utc");

        builder.Property(x => x.PaymentCompletedAtUtc)
            .HasColumnName("payment_completed_at_utc");

        builder.Property(x => x.FailedAtUtc)
            .HasColumnName("failed_at_utc");

        // Optimistic concurrency
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        // Indexes
        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("ix_order_processing_sagas_order_id");

        builder.HasIndex(x => x.CurrentState)
            .HasDatabaseName("ix_order_processing_sagas_current_state");
    }
}
