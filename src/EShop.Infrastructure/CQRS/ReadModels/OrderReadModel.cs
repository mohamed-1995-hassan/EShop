namespace EShop.Infrastructure.CQRS.ReadModels;

/// <summary>
/// Order Read Model - Denormalized view for efficient queries
/// 
/// Optimized for:
/// - Order details lookup
/// - Customer order history
/// - Status-based filtering
/// - Dashboard aggregations
/// </summary>
public class OrderReadModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "KWD";
    public int ItemCount { get; set; }
    
    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? PlacedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    public DateTime LastUpdatedAt { get; set; }
    
    // Embedded items for single-query retrieval
    public List<OrderItemReadModel> Items { get; set; } = new();
}

public class OrderItemReadModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Order summary for list views
/// </summary>
public class OrderSummaryReadModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "KWD";
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Customer order history view
/// </summary>
public class CustomerOrdersReadModel
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public List<OrderSummaryReadModel> RecentOrders { get; set; } = new();
}

/// <summary>
/// Dashboard statistics
/// </summary>
public class OrderDashboardReadModel
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public List<DailyOrderStats> DailyStats { get; set; } = new();
}

public class DailyOrderStats
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}
