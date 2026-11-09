using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.NotificationAggregate;

/// <summary>
/// Notification Aggregate - Multi-channel customer notifications
/// 
/// BUSINESS RULES:
/// - Supports Email, SMS, Push, In-App channels
/// - Priority levels affect delivery order
/// - Retry logic for failed deliveries
/// - Customer preferences respected
/// - Rate limiting per channel
/// </summary>
public class Notification : AggregateRoot<Guid>
{
    public Guid? CustomerId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? TemplateId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }

    private readonly Dictionary<string, string> _metadata = new();
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    private readonly List<NotificationDeliveryAttempt> _deliveryAttempts = new();
    public IReadOnlyCollection<NotificationDeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

    private Notification() { }

    public static Notification Create(
        Guid? customerId,
        string recipient,
        NotificationType type,
        NotificationChannel channel,
        string subject,
        string content,
        NotificationPriority priority = NotificationPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new DomainException("Recipient is required");
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Subject is required");
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Content is required");

        return new Notification
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Recipient = recipient,
            Type = type,
            Channel = channel,
            Priority = priority,
            Subject = subject,
            Content = content,
            Status = NotificationStatus.Pending,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Factory Methods

    public static Notification CreateOrderConfirmation(
        Guid customerId, 
        string email, 
        Guid orderId, 
        decimal orderTotal)
    {
        var notification = Create(
            customerId,
            email,
            NotificationType.OrderConfirmation,
            NotificationChannel.Email,
            "Order Confirmation",
            $"Your order has been confirmed. Total: {orderTotal:N2}",
            NotificationPriority.High);

        notification.SetRelatedEntity(orderId, "Order");
        notification.AddMetadata("OrderTotal", orderTotal.ToString());
        return notification;
    }

    public static Notification CreateShippingUpdate(
        Guid customerId,
        string email,
        Guid orderId,
        string trackingNumber,
        string carrier)
    {
        var notification = Create(
            customerId,
            email,
            NotificationType.ShippingUpdate,
            NotificationChannel.Email,
            "Your Order Has Shipped!",
            $"Your order is on its way! Tracking: {trackingNumber} via {carrier}",
            NotificationPriority.High);

        notification.SetRelatedEntity(orderId, "Order");
        notification.AddMetadata("TrackingNumber", trackingNumber);
        notification.AddMetadata("Carrier", carrier);
        return notification;
    }

    public static Notification CreateDeliveryNotification(
        Guid customerId,
        string phone,
        Guid orderId)
    {
        var notification = Create(
            customerId,
            phone,
            NotificationType.DeliveryNotification,
            NotificationChannel.SMS,
            "Delivery",
            "Your package has been delivered!",
            NotificationPriority.High);

        notification.SetRelatedEntity(orderId, "Order");
        return notification;
    }

    public static Notification CreatePaymentReceipt(
        Guid customerId,
        string email,
        Guid paymentId,
        decimal amount,
        string currency)
    {
        var notification = Create(
            customerId,
            email,
            NotificationType.PaymentReceipt,
            NotificationChannel.Email,
            "Payment Receipt",
            $"We received your payment of {amount:N2} {currency}",
            NotificationPriority.Normal);

        notification.SetRelatedEntity(paymentId, "Payment");
        return notification;
    }

    public static Notification CreatePromotionalEmail(
        Guid customerId,
        string email,
        string promotionName,
        string promoCode)
    {
        var notification = Create(
            customerId,
            email,
            NotificationType.Promotional,
            NotificationChannel.Email,
            promotionName,
            $"Use code {promoCode} for exclusive savings!",
            NotificationPriority.Low);

        notification.AddMetadata("PromoCode", promoCode);
        return notification;
    }

    public static Notification CreatePasswordReset(
        Guid customerId,
        string email,
        string resetToken)
    {
        var notification = Create(
            customerId,
            email,
            NotificationType.PasswordReset,
            NotificationChannel.Email,
            "Password Reset Request",
            "Click the link to reset your password. Valid for 1 hour.",
            NotificationPriority.Urgent);

        notification.AddMetadata("ResetToken", resetToken);
        return notification;
    }

    public static Notification CreateStockAlert(
        Guid customerId,
        string email,
        Guid productId,
        string productName)
    {
        var notification = Create(
            customerId,
            email,
            NotificationType.StockAlert,
            NotificationChannel.Email,
            "Back in Stock!",
            $"Great news! {productName} is back in stock.",
            NotificationPriority.Normal);

        notification.SetRelatedEntity(productId, "Product");
        notification.AddMetadata("ProductName", productName);
        return notification;
    }

    #endregion

    #region Configuration

    public void SetTemplate(string templateId)
    {
        TemplateId = templateId;
    }

    public void SetRelatedEntity(Guid entityId, string entityType)
    {
        RelatedEntityId = entityId;
        RelatedEntityType = entityType;
    }

    public void AddMetadata(string key, string value)
    {
        _metadata[key] = value;
    }

    public void ScheduleFor(DateTime scheduledTime)
    {
        if (scheduledTime <= DateTime.UtcNow)
            throw new DomainException("Scheduled time must be in the future");

        ScheduledFor = scheduledTime;
        Status = NotificationStatus.Scheduled;
    }

    #endregion

    #region Delivery

    public void MarkAsSending()
    {
        if (Status == NotificationStatus.Sent)
            throw new DomainException("Notification already sent");

        Status = NotificationStatus.Sending;
    }

    public void MarkAsSent(string? externalId = null)
    {
        AttemptCount++;
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;

        _deliveryAttempts.Add(new NotificationDeliveryAttempt(
            Guid.NewGuid(),
            AttemptCount,
            true,
            externalId));
    }

    public void MarkAsFailed(string reason)
    {
        AttemptCount++;
        FailureReason = reason;

        _deliveryAttempts.Add(new NotificationDeliveryAttempt(
            Guid.NewGuid(),
            AttemptCount,
            false,
            null,
            reason));

        // Max 3 attempts
        Status = AttemptCount >= 3 
            ? NotificationStatus.Failed 
            : NotificationStatus.PendingRetry;
    }

    public void MarkAsRead()
    {
        if (Status != NotificationStatus.Sent)
            throw new DomainException("Only sent notifications can be marked as read");

        ReadAt = DateTime.UtcNow;
        Status = NotificationStatus.Read;
    }

    public bool CanRetry()
    {
        return Status == NotificationStatus.PendingRetry && AttemptCount < 3;
    }

    public void Cancel(string reason)
    {
        if (Status == NotificationStatus.Sent || Status == NotificationStatus.Read)
            throw new DomainException("Cannot cancel sent notification");

        FailureReason = $"Cancelled: {reason}";
        Status = NotificationStatus.Cancelled;
    }

    #endregion
}

public class NotificationDeliveryAttempt : Entity<Guid>
{
    public int AttemptNumber { get; private set; }
    public bool Success { get; private set; }
    public string? ExternalId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime AttemptedAt { get; private set; }

    internal NotificationDeliveryAttempt(
        Guid id,
        int attemptNumber,
        bool success,
        string? externalId = null,
        string? failureReason = null)
    {
        Id = id;
        AttemptNumber = attemptNumber;
        Success = success;
        ExternalId = externalId;
        FailureReason = failureReason;
        AttemptedAt = DateTime.UtcNow;
    }

    private NotificationDeliveryAttempt() { } // EF Core
}

public enum NotificationType
{
    OrderConfirmation = 1,
    ShippingUpdate = 2,
    DeliveryNotification = 3,
    PaymentReceipt = 4,
    RefundProcessed = 5,
    Promotional = 6,
    PasswordReset = 7,
    AccountVerification = 8,
    StockAlert = 9,
    PriceDropAlert = 10,
    ReviewRequest = 11,
    LoyaltyPointsUpdate = 12,
    WelcomeEmail = 13,
    AbandonedCart = 14
}

public enum NotificationChannel
{
    Email = 1,
    SMS = 2,
    Push = 3,
    InApp = 4,
    WhatsApp = 5
}

public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum NotificationStatus
{
    Pending = 1,
    Scheduled = 2,
    Sending = 3,
    Sent = 4,
    Read = 5,
    Failed = 6,
    PendingRetry = 7,
    Cancelled = 8
}
