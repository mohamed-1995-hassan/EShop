using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.SubscriptionAggregate;

/// <summary>
/// Subscription Aggregate - Recurring orders and subscription boxes
/// 
/// BUSINESS RULES:
/// - Flexible billing cycles (weekly, monthly, quarterly, yearly)
/// - Pause/resume functionality
/// - Skip next delivery option
/// - Subscription discounts (typically 10-20%)
/// - Auto-renewal with payment retry
/// - Cancellation with retention offers
/// </summary>
public class Subscription : AggregateRoot<Guid>
{
    public string SubscriptionNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public SubscriptionType Type { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public int BillingDayOfMonth { get; private set; }
    public Money BasePrice { get; private set; } = null!;
    public Money DiscountAmount { get; private set; } = null!;
    public decimal DiscountPercentage { get; private set; }
    public Money TotalPrice { get; private set; } = null!;
    public Address ShippingAddress { get; private set; } = null!;
    public Guid? DefaultPaymentMethodId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? NextBillingDate { get; private set; }
    public DateTime? NextDeliveryDate { get; private set; }
    public DateTime? PausedUntil { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public int TotalDeliveries { get; private set; }
    public int SkippedDeliveries { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<SubscriptionItem> _items = new();
    public IReadOnlyCollection<SubscriptionItem> Items => _items.AsReadOnly();

    private readonly List<SubscriptionDelivery> _deliveries = new();
    public IReadOnlyCollection<SubscriptionDelivery> Deliveries => _deliveries.AsReadOnly();

    private readonly List<SubscriptionStatusChange> _statusHistory = new();
    public IReadOnlyCollection<SubscriptionStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    public bool IsActive => Status == SubscriptionStatus.Active;
    public bool IsPaused => Status == SubscriptionStatus.Paused;
    public bool CanSkipNextDelivery => IsActive && NextDeliveryDate > DateTime.UtcNow.AddDays(2);

    private Subscription() { }

    public static Subscription Create(
        Guid customerId,
        SubscriptionType type,
        BillingCycle billingCycle,
        Address shippingAddress,
        decimal discountPercentage = 10,
        int billingDayOfMonth = 1)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required");
        if (billingDayOfMonth < 1 || billingDayOfMonth > 28)
            throw new DomainException("Billing day must be between 1 and 28");

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriptionNumber = GenerateSubscriptionNumber(),
            CustomerId = customerId,
            Type = type,
            Status = SubscriptionStatus.Draft,
            BillingCycle = billingCycle,
            BillingDayOfMonth = billingDayOfMonth,
            ShippingAddress = shippingAddress,
            DiscountPercentage = discountPercentage,
            BasePrice = Money.Zero("KWD"),
            DiscountAmount = Money.Zero("KWD"),
            TotalPrice = Money.Zero("KWD"),
            TotalDeliveries = 0,
            SkippedDeliveries = 0,
            CreatedAt = DateTime.UtcNow
        };

        subscription.AddStatusChange(SubscriptionStatus.Draft, "Subscription created");
        return subscription;
    }

    #region Item Management

    public SubscriptionItem AddItem(
        Guid productId,
        string productName,
        Money unitPrice,
        int quantity,
        string? variant = null)
    {
        if (Status != SubscriptionStatus.Draft && Status != SubscriptionStatus.Active)
            throw new DomainException("Cannot modify subscription in current status");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId && i.Variant == variant);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            RecalculatePrice();
            return existingItem;
        }

        var item = new SubscriptionItem(Guid.NewGuid(), productId, productName, unitPrice, quantity, variant);
        _items.Add(item);
        RecalculatePrice();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        if (Status != SubscriptionStatus.Draft && Status != SubscriptionStatus.Active)
            throw new DomainException("Cannot modify subscription in current status");

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found");

        _items.Remove(item);
        RecalculatePrice();
    }

    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        if (Status != SubscriptionStatus.Draft && Status != SubscriptionStatus.Active)
            throw new DomainException("Cannot modify subscription in current status");

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found");

        if (quantity <= 0)
        {
            RemoveItem(itemId);
            return;
        }

        item.UpdateQuantity(quantity);
        RecalculatePrice();
    }

    private void RecalculatePrice()
    {
        BasePrice = _items.Aggregate(
            Money.Zero("KWD"),
            (total, item) => total.Add(item.UnitPrice.Multiply(item.Quantity)));

        DiscountAmount = BasePrice.Multiply(DiscountPercentage / 100);
        TotalPrice = BasePrice.Subtract(DiscountAmount);
    }

    #endregion

    #region Lifecycle Management

    public void Activate(Guid paymentMethodId)
    {
        if (Status != SubscriptionStatus.Draft)
            throw new DomainException("Only draft subscriptions can be activated");

        if (!_items.Any())
            throw new DomainException("Subscription must have at least one item");

        DefaultPaymentMethodId = paymentMethodId;
        Status = SubscriptionStatus.Active;
        StartDate = DateTime.UtcNow;
        CalculateNextDates();

        AddStatusChange(SubscriptionStatus.Active, "Subscription activated");
    }

    public void Pause(DateTime? until = null, string? reason = null)
    {
        if (!IsActive)
            throw new DomainException("Only active subscriptions can be paused");

        Status = SubscriptionStatus.Paused;
        PausedUntil = until ?? DateTime.UtcNow.AddMonths(3);

        AddStatusChange(SubscriptionStatus.Paused, reason ?? "Paused by customer");
    }

    public void Resume()
    {
        if (!IsPaused)
            throw new DomainException("Only paused subscriptions can be resumed");

        Status = SubscriptionStatus.Active;
        PausedUntil = null;
        CalculateNextDates();

        AddStatusChange(SubscriptionStatus.Active, "Subscription resumed");
    }

    public void Cancel(string reason, bool immediate = false)
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new DomainException("Subscription already cancelled");

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;

        if (immediate)
        {
            NextBillingDate = null;
            NextDeliveryDate = null;
        }

        AddStatusChange(SubscriptionStatus.Cancelled, reason);
    }

    public void Reactivate()
    {
        if (Status != SubscriptionStatus.Cancelled)
            throw new DomainException("Only cancelled subscriptions can be reactivated");

        if (CancelledAt < DateTime.UtcNow.AddDays(-90))
            throw new DomainException("Cannot reactivate subscription cancelled more than 90 days ago");

        Status = SubscriptionStatus.Active;
        CancelledAt = null;
        CancellationReason = null;
        CalculateNextDates();

        AddStatusChange(SubscriptionStatus.Active, "Subscription reactivated");
    }

    #endregion

    #region Delivery Management

    public void SkipNextDelivery(string? reason = null)
    {
        if (!CanSkipNextDelivery)
            throw new DomainException("Cannot skip next delivery");

        SkippedDeliveries++;
        CalculateNextDates();

        AddStatusChange(Status, $"Delivery skipped: {reason ?? "Customer request"}");
    }

    public SubscriptionDelivery ProcessDelivery()
    {
        if (!IsActive)
            throw new DomainException("Subscription is not active");

        var delivery = new SubscriptionDelivery(
            Guid.NewGuid(),
            TotalDeliveries + 1,
            TotalPrice,
            ShippingAddress,
            _items.Select(i => new DeliveryItem(i.ProductId, i.ProductName, i.Quantity)).ToList());

        _deliveries.Add(delivery);
        TotalDeliveries++;
        CalculateNextDates();

        return delivery;
    }

    public void RecordDeliveryShipped(Guid deliveryId, Guid orderId, string trackingNumber)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId)
            ?? throw new DomainException("Delivery not found");

        delivery.MarkAsShipped(orderId, trackingNumber);
    }

    public void RecordDeliveryCompleted(Guid deliveryId)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId)
            ?? throw new DomainException("Delivery not found");

        delivery.MarkAsCompleted();
    }

    public void RecordDeliveryFailed(Guid deliveryId, string reason)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId)
            ?? throw new DomainException("Delivery not found");

        delivery.MarkAsFailed(reason);
    }

    #endregion

    #region Configuration

    public void UpdateShippingAddress(Address newAddress)
    {
        ShippingAddress = newAddress;
    }

    public void UpdatePaymentMethod(Guid paymentMethodId)
    {
        DefaultPaymentMethodId = paymentMethodId;
    }

    public void UpdateBillingCycle(BillingCycle newCycle)
    {
        if (!IsActive && Status != SubscriptionStatus.Draft)
            throw new DomainException("Cannot modify subscription in current status");

        BillingCycle = newCycle;
        CalculateNextDates();
    }

    public void ApplyDiscount(decimal percentage)
    {
        if (percentage < 0 || percentage > 50)
            throw new DomainException("Discount must be between 0% and 50%");

        DiscountPercentage = percentage;
        RecalculatePrice();
    }

    #endregion

    #region Private Methods

    private void CalculateNextDates()
    {
        var baseDate = DateTime.UtcNow;

        NextBillingDate = BillingCycle switch
        {
            BillingCycle.Weekly => baseDate.AddDays(7),
            BillingCycle.BiWeekly => baseDate.AddDays(14),
            BillingCycle.Monthly => new DateTime(baseDate.Year, baseDate.Month, BillingDayOfMonth).AddMonths(1),
            BillingCycle.Quarterly => new DateTime(baseDate.Year, baseDate.Month, BillingDayOfMonth).AddMonths(3),
            BillingCycle.Yearly => new DateTime(baseDate.Year, baseDate.Month, BillingDayOfMonth).AddYears(1),
            _ => baseDate.AddMonths(1)
        };

        NextDeliveryDate = NextBillingDate?.AddDays(3); // Ship 3 days after billing
    }

    private void AddStatusChange(SubscriptionStatus status, string reason)
    {
        _statusHistory.Add(new SubscriptionStatusChange(Guid.NewGuid(), status, reason));
    }

    private static string GenerateSubscriptionNumber()
    {
        return $"SUB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    #endregion
}

public class SubscriptionItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string? Variant { get; private set; }

    internal SubscriptionItem(Guid id, Guid productId, string productName, Money unitPrice, int quantity, string? variant)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Variant = variant;
    }

    private SubscriptionItem() { }

    internal void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        Quantity = quantity;
    }

    internal void UpdatePrice(Money newPrice) => UnitPrice = newPrice;
}

public class SubscriptionDelivery : Entity<Guid>
{
    public int DeliveryNumber { get; private set; }
    public Money Amount { get; private set; }
    public Address ShippingAddress { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public Guid? OrderId { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<DeliveryItem> _items;
    public IReadOnlyCollection<DeliveryItem> Items => _items.AsReadOnly();

    internal SubscriptionDelivery(Guid id, int deliveryNumber, Money amount, Address shippingAddress, List<DeliveryItem> items)
    {
        Id = id;
        DeliveryNumber = deliveryNumber;
        Amount = amount;
        ShippingAddress = shippingAddress;
        Status = DeliveryStatus.Pending;
        ScheduledAt = DateTime.UtcNow;
        _items = items;
    }

    private SubscriptionDelivery() { _items = new(); }

    internal void MarkAsShipped(Guid orderId, string trackingNumber)
    {
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        Status = DeliveryStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
    }

    internal void MarkAsCompleted()
    {
        Status = DeliveryStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkAsFailed(string reason)
    {
        Status = DeliveryStatus.Failed;
        FailureReason = reason;
    }
}

public class DeliveryItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }

    public DeliveryItem(Guid productId, string productName, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
    }
}

public class SubscriptionStatusChange : Entity<Guid>
{
    public SubscriptionStatus Status { get; private set; }
    public string Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    internal SubscriptionStatusChange(Guid id, SubscriptionStatus status, string reason)
    {
        Id = id;
        Status = status;
        Reason = reason;
        ChangedAt = DateTime.UtcNow;
    }

    private SubscriptionStatusChange() { }
}

public enum SubscriptionType
{
    ProductReplenishment = 1,  // Same products each time
    SubscriptionBox = 2,       // Curated box
    MealKit = 3,
    Magazine = 4,
    Software = 5,
    Membership = 6
}

public enum SubscriptionStatus
{
    Draft = 1,
    Active = 2,
    Paused = 3,
    PastDue = 4,
    Cancelled = 5,
    Expired = 6
}

public enum BillingCycle
{
    Weekly = 1,
    BiWeekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Yearly = 5
}

public enum DeliveryStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Completed = 4,
    Failed = 5,
    Skipped = 6
}
