namespace EShop.Infrastructure.Messaging.IntegrationEvents;

/// <summary>
/// Base class for Integration Events.
/// 
/// INTEGRATION EVENTS vs DOMAIN EVENTS:
/// - Domain Events: Internal to a bounded context, synchronous, in-memory
/// - Integration Events: Cross bounded contexts, asynchronous, via message broker
/// 
/// Integration Events are published to RabbitMQ and consumed by other services.
/// </summary>
public abstract class IntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// Used for idempotency (inbox pattern).
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// When the event occurred in the source system.
    /// </summary>
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Correlation ID for distributed tracing.
    /// Follows a request across multiple services.
    /// </summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Conversation ID for saga/process tracking.
    /// Groups related events in a long-running process.
    /// </summary>
    public Guid? ConversationId { get; init; }
    
    /// <summary>
    /// Source service/bounded context that published this event.
    /// </summary>
    public string Source { get; init; } = "EShop.Orders";
}

#region Order Integration Events

/// <summary>
/// Published when an order is successfully placed.
/// Consumers: Inventory, Payment, Notification, Analytics services
/// </summary>
public class OrderPlacedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "KWD";
    public List<OrderItemDto> Items { get; init; } = new();
    public AddressDto ShippingAddress { get; init; } = null!;
}

/// <summary>
/// Published when an order is shipped.
/// Consumers: Notification, Tracking services
/// </summary>
public class OrderShippedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string? TrackingNumber { get; init; }
    public string? Carrier { get; init; }
    public DateTime EstimatedDeliveryUtc { get; init; }
}

/// <summary>
/// Published when an order is cancelled.
/// Consumers: Inventory (release stock), Payment (refund), Notification services
/// </summary>
public class OrderCancelledIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public decimal RefundAmount { get; init; }
    public List<OrderItemDto> ItemsToRelease { get; init; } = new();
}

#endregion

#region Inventory Integration Events

/// <summary>
/// Published when stock is reserved for an order.
/// </summary>
public class StockReservedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public List<ReservedItemDto> ReservedItems { get; init; } = new();
}

/// <summary>
/// Published when stock reservation fails (insufficient stock).
/// Triggers order cancellation saga.
/// </summary>
public class StockReservationFailedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public List<FailedItemDto> FailedItems { get; init; } = new();
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Published when reserved stock is released (order cancelled).
/// </summary>
public class StockReleasedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public List<ReleasedItemDto> ReleasedItems { get; init; } = new();
}

#endregion

#region Payment Integration Events

/// <summary>
/// Published when payment is successfully processed.
/// </summary>
public class PaymentCompletedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "KWD";
    public string PaymentMethod { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
}

/// <summary>
/// Published when payment fails.
/// Triggers order cancellation and stock release.
/// </summary>
public class PaymentFailedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
}

/// <summary>
/// Published when refund is processed.
/// </summary>
public class RefundCompletedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid RefundId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "KWD";
}

#endregion

#region DTOs for Integration Events

public class OrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public class AddressDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
}

public class ReservedItemDto
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public int QuantityReserved { get; init; }
}

public class FailedItemDto
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public int RequestedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
}

public class ReleasedItemDto
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public int QuantityReleased { get; init; }
}

#endregion
