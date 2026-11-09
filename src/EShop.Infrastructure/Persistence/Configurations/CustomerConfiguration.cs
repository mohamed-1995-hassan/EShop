using EShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Customer entity.
/// 
/// Demonstrates:
/// - Optional Value Object (nullable Address)
/// - Unique constraints
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        // Primary Key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Simple Properties
        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // ========================================
        // COMPLEX TYPE: ShippingAddress (Optional Value Object)
        // ========================================
        builder.OwnsOne(c => c.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasColumnName("shipping_street")
                .HasMaxLength(200);

            addressBuilder.Property(a => a.City)
                .HasColumnName("shipping_city")
                .HasMaxLength(100);

            addressBuilder.Property(a => a.Country)
                .HasColumnName("shipping_country")
                .HasMaxLength(100);

            addressBuilder.Property(a => a.PostalCode)
                .HasColumnName("shipping_postal_code")
                .HasMaxLength(20);
        });

        // Indexes
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("ix_customers_email_unique");
    }
}
