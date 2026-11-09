using EShop.Domain.Common;
using EShop.Domain.Events;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.ProductAggregate;

/// <summary>
/// Product Aggregate Root
/// 
/// BUSINESS RULES:
/// - Price cannot be negative
/// - Stock cannot go negative
/// - Low stock threshold triggers domain event
/// - Discontinued products cannot be restocked
/// </summary>
public class Product : AggregateRoot<Guid>
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public int LowStockThreshold { get; private set; }
    public bool IsDiscontinued { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Categories as Value Object collection
    private readonly List<ProductCategory> _categories = new();
    public IReadOnlyCollection<ProductCategory> Categories => _categories.AsReadOnly();

    private Product() { } // For EF Core

    public static Product Create(
        string sku,
        string name,
        string description,
        Money price,
        int initialStock = 0,
        int lowStockThreshold = 10)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Product SKU is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");
        if (initialStock < 0)
            throw new DomainException("Initial stock cannot be negative");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.ToUpperInvariant(),
            Name = name,
            Description = description ?? string.Empty,
            Price = price,
            StockQuantity = initialStock,
            LowStockThreshold = lowStockThreshold,
            IsDiscontinued = false,
            CreatedAt = DateTime.UtcNow
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Sku, product.Name));

        return product;
    }

    public void UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");

        Name = name;
        Description = description ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(Money newPrice)
    {
        var oldPrice = Price;
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductPriceChangedEvent(Id, Sku, oldPrice.Amount, newPrice.Amount, newPrice.Currency));
    }

    /// <summary>
    /// Add stock - typically from receiving inventory
    /// </summary>
    public void AddStock(int quantity, string? reference = null)
    {
        if (IsDiscontinued)
            throw new DomainException("Cannot add stock to discontinued product");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductStockAddedEvent(Id, Sku, quantity, StockQuantity, reference));
    }

    /// <summary>
    /// Check if the requested quantity can be reserved
    /// </summary>
    public bool CanReserve(int quantity)
    {
        return !IsDiscontinued && quantity > 0 && StockQuantity >= quantity;
    }

    /// <summary>
    /// Reserve stock without order reference (saga command)
    /// </summary>
    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        // Check for low stock condition
        if (StockQuantity <= LowStockThreshold && StockQuantity > 0)
        {
            AddDomainEvent(new ProductLowStockEvent(Id, Sku, StockQuantity, LowStockThreshold));
        }
        else if (StockQuantity == 0)
        {
            AddDomainEvent(new ProductOutOfStockEvent(Id, Sku));
        }
    }

    /// <summary>
    /// Reserve stock for an order
    /// </summary>
    public void ReserveStock(int quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        // Check for low stock condition
        if (StockQuantity <= LowStockThreshold && StockQuantity > 0)
        {
            AddDomainEvent(new ProductLowStockEvent(Id, Sku, StockQuantity, LowStockThreshold));
        }
        else if (StockQuantity == 0)
        {
            AddDomainEvent(new ProductOutOfStockEvent(Id, Sku));
        }

        AddDomainEvent(new ProductStockReservedEvent(Id, Sku, quantity, orderId, StockQuantity));
    }

    /// <summary>
    /// Release reserved stock (e.g., order cancelled)
    /// </summary>
    public void ReleaseStock(int quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductStockReleasedEvent(Id, Sku, quantity, orderId, StockQuantity));
    }

    public void AddCategory(string categoryName)
    {
        if (_categories.Any(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase)))
            return;

        _categories.Add(new ProductCategory(categoryName));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCategory(string categoryName)
    {
        var category = _categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        if (category != null)
        {
            _categories.Remove(category);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Discontinue()
    {
        if (IsDiscontinued)
            throw new DomainException("Product is already discontinued");

        IsDiscontinued = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductDiscontinuedEvent(Id, Sku, StockQuantity));
    }

    public void Reactivate()
    {
        if (!IsDiscontinued)
            throw new DomainException("Product is not discontinued");

        IsDiscontinued = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Value Object for product category
/// </summary>
public class ProductCategory : ValueObject
{
    public string Name { get; }

    private ProductCategory() { } // For EF Core

    public ProductCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required");

        Name = name.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name.ToLowerInvariant();
    }
}
