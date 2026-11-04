using EShop.Domain.Common;

namespace EShop.Domain.Events;

/// <summary>
/// Domain Event - raised when an item is added to an order.
/// 
/// HANDLERS CAN:
/// - Update real-time inventory availability
/// - Trigger cross-sell/upsell recommendations
/// - Update shopping cart analytics
/// </summary>
public class OrderItemAddedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    
    public OrderItemAddedEvent(
        Guid orderId,
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
