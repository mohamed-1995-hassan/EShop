using EShop.Domain.Common;

namespace EShop.Domain.Events;

/// <summary>
/// Domain Event - raised when an order is created (before placement).
/// Used for analytics and tracking.
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    
    public OrderCreatedEvent(Guid orderId, Guid customerId, decimal totalAmount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }
}
