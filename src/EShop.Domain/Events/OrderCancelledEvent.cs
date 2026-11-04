using EShop.Domain.Common;

namespace EShop.Domain.Events;

/// <summary>
/// Domain Event - raised when an order is cancelled.
/// 
/// HANDLERS CAN:
/// - Release reserved inventory
/// - Send cancellation email
/// - Process refund if payment was made
/// - Update analytics
/// </summary>
public class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public string Reason { get; }
    public decimal RefundAmount { get; }
    
    public OrderCancelledEvent(
        Guid orderId, 
        Guid customerId,
        string reason,
        decimal refundAmount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        Reason = reason;
        RefundAmount = refundAmount;
    }
}
