using EShop.Domain.Common;

namespace EShop.Domain.Events;

/// <summary>
/// Domain Event - raised when an order is confirmed after payment.
/// 
/// HANDLERS CAN:
/// - Start fulfillment process
/// - Update inventory to committed
/// - Send payment confirmation to customer
/// </summary>
public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    
    public OrderConfirmedEvent(Guid orderId, Guid customerId, decimal totalAmount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }
}
