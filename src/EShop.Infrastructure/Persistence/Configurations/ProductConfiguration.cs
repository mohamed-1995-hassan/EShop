using EShop.Domain.Aggregates.ProductAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Product aggregate.
/// 
/// Demonstrates:
/// - Complex Type for Money Value Object
/// - Collection of Value Objects (Categories)
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        // Primary Key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Simple Properties
        builder.Property(p => p.Sku)
            .HasColumnName("sku")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(p => p.StockQuantity)
            .HasColumnName("stock_quantity")
            .IsRequired();

        builder.Property(p => p.LowStockThreshold)
            .HasColumnName("low_stock_threshold")
            .HasDefaultValue(10);

        builder.Property(p => p.IsDiscontinued)
            .HasColumnName("is_discontinued")
            .HasDefaultValue(false);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        // ========================================
        // COMPLEX TYPE: Price (Money Value Object)
        // ========================================
        builder.OwnsOne(p => p.Price, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 3)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // ========================================
        // COLLECTION OF VALUE OBJECTS: Categories
        // ========================================
        // EF Core 8+ can map owned collections
        builder.OwnsMany(p => p.Categories, categoryBuilder =>
        {
            categoryBuilder.ToTable("product_categories");

            categoryBuilder.WithOwner().HasForeignKey("ProductId");

            categoryBuilder.Property<int>("Id")
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            categoryBuilder.HasKey("Id");

            categoryBuilder.Property(c => c.Name)
                .HasColumnName("category_name")
                .HasMaxLength(100)
                .IsRequired();

            categoryBuilder.HasIndex("ProductId", "Name")
                .IsUnique()
                .HasDatabaseName("ix_product_categories_product_name_unique");
        });

        // Access private backing field for encapsulated collection
        builder.Navigation(p => p.Categories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("ix_products_sku_unique");

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("ix_products_name");

        builder.HasIndex(p => p.IsDiscontinued)
            .HasDatabaseName("ix_products_is_discontinued");
    }
}
