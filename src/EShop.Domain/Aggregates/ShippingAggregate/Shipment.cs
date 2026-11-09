using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.ShippingAggregate;

/// <summary>
/// Shipment Aggregate Root - Tracks package delivery
/// 
/// BUSINESS RULES:
/// - Shipment must have valid carrier and tracking number
/// - Status transitions follow defined workflow
/// - Delivery attempts tracked with reasons
/// - Signature required for high-value items
/// - Auto-complete after delivery confirmation
/// </summary>
public class Shipment : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public string TrackingNumber { get; private set; } = string.Empty;
    public Guid CarrierId { get; private set; }
    public string CarrierName { get; private set; } = string.Empty;
    public ShipmentStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; } = null!;
    public Address? ReturnAddress { get; private set; }
    public ShipmentType Type { get; private set; }
    public decimal Weight { get; private set; }
    public decimal DeclaredValue { get; private set; }
    public bool SignatureRequired { get; private set; }
    public bool InsuranceRequired { get; private set; }
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    public string? DeliveryInstructions { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }

    private readonly List<ShipmentItem> _items = new();
    public IReadOnlyCollection<ShipmentItem> Items => _items.AsReadOnly();

    private readonly List<TrackingEvent> _trackingEvents = new();
    public IReadOnlyCollection<TrackingEvent> TrackingEvents => _trackingEvents.AsReadOnly();

    private readonly List<DeliveryAttempt> _deliveryAttempts = new();
    public IReadOnlyCollection<DeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

    private Shipment() { }

    public static Shipment Create(
        Guid orderId,
        Guid carrierId,
        string carrierName,
        Address shippingAddress,
        ShipmentType type = ShipmentType.Standard,
        string? deliveryInstructions = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required");

        return new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CarrierId = carrierId,
            CarrierName = carrierName,
            ShippingAddress = shippingAddress,
            Type = type,
            Status = ShipmentStatus.Pending,
            DeliveryInstructions = deliveryInstructions,
            CreatedAt = DateTime.UtcNow,
            SignatureRequired = false,
            InsuranceRequired = false
        };
    }

    #region Item Management

    public void AddItem(Guid productId, string productName, int quantity, decimal weight)
    {
        if (Status != ShipmentStatus.Pending)
            throw new DomainException("Cannot modify shipment after dispatch");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            _items.Add(new ShipmentItem(Guid.NewGuid(), productId, productName, quantity, weight));
        }

        RecalculateWeight();
    }

    private void RecalculateWeight()
    {
        Weight = _items.Sum(i => i.Weight * i.Quantity);
    }

    #endregion

    #region Configuration

    public void SetDeclaredValue(decimal value)
    {
        DeclaredValue = value;
        
        // Auto-enable signature for high-value shipments
        if (value > 500)
        {
            SignatureRequired = true;
            InsuranceRequired = true;
        }
    }

    public void SetReturnAddress(Address returnAddress)
    {
        ReturnAddress = returnAddress;
    }

    public void RequireSignature() => SignatureRequired = true;
    public void RequireInsurance() => InsuranceRequired = true;

    public void SetEstimatedDelivery(DateTime estimatedDate)
    {
        if (estimatedDate < DateTime.UtcNow)
            throw new DomainException("Estimated delivery must be in the future");
        
        EstimatedDeliveryDate = estimatedDate;
    }

    #endregion

    #region Status Transitions

    public void AssignTrackingNumber(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new DomainException("Tracking number is required");

        TrackingNumber = trackingNumber;
        AddTrackingEvent("Label created", "Shipping label has been created");
    }

    public void MarkAsReadyForPickup()
    {
        if (Status != ShipmentStatus.Pending)
            throw new DomainException("Shipment must be pending");

        if (string.IsNullOrWhiteSpace(TrackingNumber))
            throw new DomainException("Tracking number required before pickup");

        if (!_items.Any())
            throw new DomainException("Shipment must have at least one item");

        Status = ShipmentStatus.ReadyForPickup;
        AddTrackingEvent("Ready for pickup", "Package is ready for carrier pickup");
    }

    public void MarkAsPickedUp(string? driverName = null)
    {
        if (Status != ShipmentStatus.ReadyForPickup)
            throw new DomainException("Shipment must be ready for pickup");

        Status = ShipmentStatus.PickedUp;
        ShippedAt = DateTime.UtcNow;
        
        var details = string.IsNullOrWhiteSpace(driverName) 
            ? "Package picked up by carrier" 
            : $"Package picked up by {driverName}";
        AddTrackingEvent("Picked up", details);
    }

    public void MarkAsInTransit(string location, string? details = null)
    {
        if (Status != ShipmentStatus.PickedUp && Status != ShipmentStatus.InTransit)
            throw new DomainException("Invalid status transition");

        Status = ShipmentStatus.InTransit;
        AddTrackingEvent($"In transit - {location}", details ?? "Package in transit");
    }

    public void MarkAsOutForDelivery()
    {
        if (Status != ShipmentStatus.InTransit)
            throw new DomainException("Shipment must be in transit");

        Status = ShipmentStatus.OutForDelivery;
        AddTrackingEvent("Out for delivery", "Package is out for delivery");
    }

    public void MarkAsDelivered(string? receiverName = null, string? signatureImage = null)
    {
        if (Status != ShipmentStatus.OutForDelivery)
            throw new DomainException("Shipment must be out for delivery");

        if (SignatureRequired && string.IsNullOrWhiteSpace(signatureImage))
            throw new DomainException("Signature is required for delivery");

        Status = ShipmentStatus.Delivered;
        ActualDeliveryDate = DateTime.UtcNow;

        var details = string.IsNullOrWhiteSpace(receiverName)
            ? "Package delivered"
            : $"Package delivered to {receiverName}";
        AddTrackingEvent("Delivered", details);
    }

    public void RecordDeliveryAttempt(string reason)
    {
        if (Status != ShipmentStatus.OutForDelivery)
            throw new DomainException("Shipment must be out for delivery");

        var attempt = new DeliveryAttempt(
            Guid.NewGuid(), 
            _deliveryAttempts.Count + 1,
            reason);
        _deliveryAttempts.Add(attempt);

        AddTrackingEvent("Delivery attempted", $"Attempt #{attempt.AttemptNumber}: {reason}");

        // After 3 failed attempts, return to sender
        if (_deliveryAttempts.Count >= 3)
        {
            Status = ShipmentStatus.ReturnToSender;
            AddTrackingEvent("Return to sender", "Maximum delivery attempts exceeded");
        }
        else
        {
            Status = ShipmentStatus.InTransit; // Back to transit for reattempt
        }
    }

    public void MarkAsReturnToSender(string reason)
    {
        Status = ShipmentStatus.ReturnToSender;
        AddTrackingEvent("Return to sender", reason);
    }

    public void MarkAsException(string reason)
    {
        Status = ShipmentStatus.Exception;
        AddTrackingEvent("Exception", reason);
    }

    #endregion

    private void AddTrackingEvent(string status, string details)
    {
        _trackingEvents.Add(new TrackingEvent(Guid.NewGuid(), status, details));
    }
}

public class ShipmentItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal Weight { get; private set; }

    internal ShipmentItem(Guid id, Guid productId, string productName, int quantity, decimal weight)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        Weight = weight;
    }

    private ShipmentItem() { } // EF Core

    internal void UpdateQuantity(int quantity) => Quantity = quantity;
}

public class TrackingEvent : Entity<Guid>
{
    public string Status { get; private set; }
    public string Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    internal TrackingEvent(Guid id, string status, string details)
    {
        Id = id;
        Status = status;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }

    private TrackingEvent() { } // EF Core
}

public class DeliveryAttempt : Entity<Guid>
{
    public int AttemptNumber { get; private set; }
    public string FailureReason { get; private set; }
    public DateTime AttemptedAt { get; private set; }

    internal DeliveryAttempt(Guid id, int attemptNumber, string failureReason)
    {
        Id = id;
        AttemptNumber = attemptNumber;
        FailureReason = failureReason;
        AttemptedAt = DateTime.UtcNow;
    }

    private DeliveryAttempt() { } // EF Core
}

public enum ShipmentStatus
{
    Pending = 1,
    ReadyForPickup = 2,
    PickedUp = 3,
    InTransit = 4,
    OutForDelivery = 5,
    Delivered = 6,
    ReturnToSender = 7,
    Exception = 8,
    Cancelled = 9
}

public enum ShipmentType
{
    Standard = 1,
    Express = 2,
    NextDay = 3,
    SameDay = 4,
    Economy = 5,
    Freight = 6
}
