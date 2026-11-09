using EShop.Domain.Aggregates.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for OrderLine entity.
/// 
/// Demonstrates:
/// - Entity within aggregate (not aggregate root)
/// - Multiple Value Objects on same entity
/// </summary>
public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");

        // Primary Key
        builder.HasKey(ol => ol.Id);
        builder.Property(ol => ol.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign Key to Order (shadow property)
        builder.Property<Guid>("OrderId")
            .HasColumnName("order_id")
            .IsRequired();

        // Simple Properties
        builder.Property(ol => ol.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(ol => ol.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ol => ol.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        // ========================================
        // COMPLEX TYPE: UnitPrice (Money Value Object)
        // ========================================
        builder.OwnsOne(ol => ol.UnitPrice, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("unit_price_amount")
                .HasPrecision(18, 3)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("unit_price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // ========================================
        // COMPLEX TYPE: LineTotal (Money Value Object)
        // ========================================
        builder.OwnsOne(ol => ol.LineTotal, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("line_total_amount")
                .HasPrecision(18, 3)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("line_total_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Indexes
        builder.HasIndex("OrderId").HasDatabaseName("ix_order_lines_order_id");
        builder.HasIndex(ol => ol.ProductId).HasDatabaseName("ix_order_lines_product_id");
    }
}
