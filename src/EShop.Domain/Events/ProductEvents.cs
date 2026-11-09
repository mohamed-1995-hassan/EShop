using EShop.Domain.Common;

namespace EShop.Domain.Events;

/// <summary>
/// Product Created Event
/// </summary>
public class ProductCreatedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public string Name { get; }

    public ProductCreatedEvent(Guid productId, string sku, string name)
    {
        ProductId = productId;
        Sku = sku;
        Name = name;
    }
}

/// <summary>
/// Product Price Changed Event - triggers notifications, price history
/// </summary>
public class ProductPriceChangedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }
    public string Currency { get; }

    public ProductPriceChangedEvent(Guid productId, string sku, decimal oldPrice, decimal newPrice, string currency)
    {
        ProductId = productId;
        Sku = sku;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        Currency = currency;
    }
}

/// <summary>
/// Stock Added Event - from receiving inventory
/// </summary>
public class ProductStockAddedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public int QuantityAdded { get; }
    public int NewStockLevel { get; }
    public string? Reference { get; }

    public ProductStockAddedEvent(Guid productId, string sku, int quantityAdded, int newStockLevel, string? reference)
    {
        ProductId = productId;
        Sku = sku;
        QuantityAdded = quantityAdded;
        NewStockLevel = newStockLevel;
        Reference = reference;
    }
}

/// <summary>
/// Stock Reserved Event - when order reserves inventory
/// </summary>
public class ProductStockReservedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public int QuantityReserved { get; }
    public Guid OrderId { get; }
    public int RemainingStock { get; }

    public ProductStockReservedEvent(Guid productId, string sku, int quantityReserved, Guid orderId, int remainingStock)
    {
        ProductId = productId;
        Sku = sku;
        QuantityReserved = quantityReserved;
        OrderId = orderId;
        RemainingStock = remainingStock;
    }
}

/// <summary>
/// Stock Released Event - when order is cancelled
/// </summary>
public class ProductStockReleasedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public int QuantityReleased { get; }
    public Guid OrderId { get; }
    public int NewStockLevel { get; }

    public ProductStockReleasedEvent(Guid productId, string sku, int quantityReleased, Guid orderId, int newStockLevel)
    {
        ProductId = productId;
        Sku = sku;
        QuantityReleased = quantityReleased;
        OrderId = orderId;
        NewStockLevel = newStockLevel;
    }
}

/// <summary>
/// Low Stock Alert Event - triggers reorder notifications
/// </summary>
public class ProductLowStockEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public int CurrentStock { get; }
    public int Threshold { get; }

    public ProductLowStockEvent(Guid productId, string sku, int currentStock, int threshold)
    {
        ProductId = productId;
        Sku = sku;
        CurrentStock = currentStock;
        Threshold = threshold;
    }
}

/// <summary>
/// Out of Stock Event - triggers urgent notifications
/// </summary>
public class ProductOutOfStockEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }

    public ProductOutOfStockEvent(Guid productId, string sku)
    {
        ProductId = productId;
        Sku = sku;
    }
}

/// <summary>
/// Product Discontinued Event
/// </summary>
public class ProductDiscontinuedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public int RemainingStock { get; }

    public ProductDiscontinuedEvent(Guid productId, string sku, int remainingStock)
    {
        ProductId = productId;
        Sku = sku;
        RemainingStock = remainingStock;
    }
}
