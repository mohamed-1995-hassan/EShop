using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Order aggregate.
/// 
/// Demonstrates:
/// - Complex Type (Owned Entity) mapping for Value Objects
/// - Collection of entities within aggregate
/// - Private field access for encapsulated collections
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        // Primary Key
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // We generate IDs in domain

        // Simple Properties
        builder.Property(o => o.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(o => o.OrderDate)
            .HasColumnName("order_date")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Version)
            .HasColumnName("version")
            .IsConcurrencyToken(); // Optimistic concurrency

        // ========================================
        // COMPLEX TYPE: ShippingAddress (Value Object)
        // ========================================
        // EF Core 8+ uses OwnsOne for complex types
        builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasColumnName("shipping_street")
                .HasMaxLength(200)
                .IsRequired();

            addressBuilder.Property(a => a.City)
                .HasColumnName("shipping_city")
                .HasMaxLength(100)
                .IsRequired();

            addressBuilder.Property(a => a.Country)
                .HasColumnName("shipping_country")
                .HasMaxLength(100)
                .IsRequired();

            addressBuilder.Property(a => a.PostalCode)
                .HasColumnName("shipping_postal_code")
                .HasMaxLength(20);
        });

        // ========================================
        // COMPLEX TYPE: TotalAmount (Money Value Object)
        // ========================================
        builder.OwnsOne(o => o.TotalAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 3) // Support KWD (3 decimal places)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("total_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // ========================================
        // COLLECTION: OrderLines (Entities within Aggregate)
        // ========================================
        // Access private backing field for encapsulated collection
        builder.HasMany(o => o.OrderLines)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        // Use backing field to allow EF to populate private collection
        builder.Navigation(o => o.OrderLines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(o => o.CustomerId).HasDatabaseName("ix_orders_customer_id");
        builder.HasIndex(o => o.Status).HasDatabaseName("ix_orders_status");
        builder.HasIndex(o => o.OrderDate).HasDatabaseName("ix_orders_order_date");
    }
}
