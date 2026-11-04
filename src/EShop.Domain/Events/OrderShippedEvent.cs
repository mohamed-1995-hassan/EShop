using EShop.Domain.Common;

namespace EShop.Domain.Events;

/// <summary>
/// Domain Event - raised when an order is shipped.
/// 
/// HANDLERS CAN:
/// - Send shipping notification to customer
/// - Update order tracking system
/// - Trigger delivery scheduling
/// </summary>
public class OrderShippedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string? TrackingNumber { get; }
    public string? Carrier { get; }
    public DateTime EstimatedDelivery { get; }
    
    public OrderShippedEvent(
        Guid orderId, 
        string? trackingNumber = null,
        string? carrier = null,
        DateTime? estimatedDelivery = null)
    {
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        Carrier = carrier;
        EstimatedDelivery = estimatedDelivery ?? DateTime.UtcNow.AddDays(3);
    }
}
