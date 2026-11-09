namespace EShop.Infrastructure.Messaging.Sagas;

/// <summary>
/// Commands published by the saga to coordinate services.
/// Commands are routed to specific consumers, unlike events which are broadcast.
/// </summary>

#region Stock Commands

/// <summary>
/// Command to reserve stock for an order.
/// Sent to Inventory Service.
/// </summary>
public class ReserveStockCommand
{
    public Guid OrderId { get; init; }
    public List<ReserveStockItem> Items { get; init; } = new();
    public Guid CorrelationId { get; init; }
    public Guid? ConversationId { get; init; }
}

public class ReserveStockItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Command to release reserved stock (compensation).
/// Sent when payment fails to rollback stock reservation.
/// </summary>
public class ReleaseStockCommand
{
    public Guid OrderId { get; init; }
    public Guid CorrelationId { get; init; }
}

#endregion

#region Payment Commands

/// <summary>
/// Command to process payment for an order.
/// Sent to Payment Service after successful stock reservation.
/// </summary>
public class ProcessPaymentCommand
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "KWD";
    public Guid CorrelationId { get; init; }
    public Guid? ConversationId { get; init; }
}

#endregion

#region Order Commands

/// <summary>
/// Command to confirm an order after successful payment.
/// </summary>
public class ConfirmOrderCommand
{
    public Guid OrderId { get; init; }
    public Guid CorrelationId { get; init; }
}

/// <summary>
/// Command to cancel an order (compensation).
/// Sent when stock reservation or payment fails.
/// </summary>
public class CancelOrderCommand
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Guid CorrelationId { get; init; }
}

#endregion
