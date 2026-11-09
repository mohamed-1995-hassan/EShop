using EShop.Domain.Common;

namespace EShop.Domain.Events;

public class ShipmentCreatedEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public Guid CarrierId { get; }
    public string CarrierName { get; }

    public ShipmentCreatedEvent(Guid shipmentId, Guid orderId, Guid carrierId, string carrierName)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        CarrierId = carrierId;
        CarrierName = carrierName;
    }
}

public class ShipmentLabelGeneratedEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public string LabelUrl { get; }

    public ShipmentLabelGeneratedEvent(Guid shipmentId, Guid orderId, string trackingNumber, string labelUrl)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        LabelUrl = labelUrl;
    }
}

public class ShipmentPickedUpEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public string CarrierName { get; }
    public DateTime PickedUpAt { get; }

    public ShipmentPickedUpEvent(Guid shipmentId, Guid orderId, string trackingNumber, string carrierName, DateTime pickedUpAt)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        CarrierName = carrierName;
        PickedUpAt = pickedUpAt;
    }
}

public class ShipmentInTransitEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public string Location { get; }
    public DateTime Timestamp { get; }

    public ShipmentInTransitEvent(Guid shipmentId, Guid orderId, string trackingNumber, string location, DateTime timestamp)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        Location = location;
        Timestamp = timestamp;
    }
}

public class ShipmentOutForDeliveryEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public DateTime? EstimatedDeliveryTime { get; }

    public ShipmentOutForDeliveryEvent(Guid shipmentId, Guid orderId, string trackingNumber, DateTime? estimatedDeliveryTime)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        EstimatedDeliveryTime = estimatedDeliveryTime;
    }
}

public class ShipmentDeliveredEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public DateTime DeliveredAt { get; }
    public string? ReceiverName { get; }
    public bool SignatureCollected { get; }

    public ShipmentDeliveredEvent(Guid shipmentId, Guid orderId, string trackingNumber, DateTime deliveredAt, string? receiverName, bool signatureCollected)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        DeliveredAt = deliveredAt;
        ReceiverName = receiverName;
        SignatureCollected = signatureCollected;
    }
}

public class ShipmentDeliveryFailedEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public int AttemptNumber { get; }
    public string FailureReason { get; }

    public ShipmentDeliveryFailedEvent(Guid shipmentId, Guid orderId, string trackingNumber, int attemptNumber, string failureReason)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        AttemptNumber = attemptNumber;
        FailureReason = failureReason;
    }
}

public class ShipmentReturnToSenderEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public string Reason { get; }

    public ShipmentReturnToSenderEvent(Guid shipmentId, Guid orderId, string trackingNumber, string reason)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        Reason = reason;
    }
}

public class ShipmentExceptionEvent : DomainEvent
{
    public Guid ShipmentId { get; }
    public Guid OrderId { get; }
    public string TrackingNumber { get; }
    public string ExceptionType { get; }
    public string Description { get; }

    public ShipmentExceptionEvent(Guid shipmentId, Guid orderId, string trackingNumber, string exceptionType, string description)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        ExceptionType = exceptionType;
        Description = description;
    }
}
