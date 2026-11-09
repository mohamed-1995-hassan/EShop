using EShop.Domain.Aggregates.PaymentAggregate;
using EShop.Domain.Common;

namespace EShop.Domain.Events;

public class PaymentInitiatedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public PaymentMethodType PaymentMethod { get; }

    public PaymentInitiatedEvent(Guid paymentId, Guid orderId, Guid customerId, decimal amount, string currency, PaymentMethodType paymentMethod)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        PaymentMethod = paymentMethod;
    }
}

public class PaymentAuthorizedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string GatewayTransactionId { get; }

    public PaymentAuthorizedEvent(Guid paymentId, Guid orderId, decimal amount, string currency, string gatewayTransactionId)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        GatewayTransactionId = gatewayTransactionId;
    }
}

public class PaymentCapturedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string GatewayTransactionId { get; }

    public PaymentCapturedEvent(Guid paymentId, Guid orderId, Guid customerId, decimal amount, string currency, string gatewayTransactionId)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        GatewayTransactionId = gatewayTransactionId;
    }
}

public class PaymentFailedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string FailureReason { get; }
    public int AttemptCount { get; }

    public PaymentFailedEvent(Guid paymentId, Guid orderId, decimal amount, string currency, string failureReason, int attemptCount)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        FailureReason = failureReason;
        AttemptCount = attemptCount;
    }
}

public class PaymentVoidedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public string Reason { get; }

    public PaymentVoidedEvent(Guid paymentId, Guid orderId, decimal amount, string reason)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        Reason = reason;
    }
}

public class RefundRequestedEvent : DomainEvent
{
    public Guid RefundId { get; }
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public RefundType Type { get; }
    public string Reason { get; }

    public RefundRequestedEvent(Guid refundId, Guid paymentId, Guid orderId, Guid customerId, decimal amount, string currency, RefundType type, string reason)
    {
        RefundId = refundId;
        PaymentId = paymentId;
        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Type = type;
        Reason = reason;
    }
}

public class RefundProcessedEvent : DomainEvent
{
    public Guid RefundId { get; }
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string GatewayRefundId { get; }

    public RefundProcessedEvent(Guid refundId, Guid paymentId, Guid orderId, Guid customerId, decimal amount, string currency, string gatewayRefundId)
    {
        RefundId = refundId;
        PaymentId = paymentId;
        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        GatewayRefundId = gatewayRefundId;
    }
}

public class RefundRejectedEvent : DomainEvent
{
    public Guid RefundId { get; }
    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public string RejectionReason { get; }

    public RefundRejectedEvent(Guid refundId, Guid paymentId, decimal amount, string rejectionReason)
    {
        RefundId = refundId;
        PaymentId = paymentId;
        Amount = amount;
        RejectionReason = rejectionReason;
    }
}

public class PaymentMethodAddedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public PaymentMethodType Type { get; }
    public string LastFourDigits { get; }
    public bool IsDefault { get; }

    public PaymentMethodAddedEvent(Guid customerId, PaymentMethodType type, string lastFourDigits, bool isDefault)
    {
        CustomerId = customerId;
        Type = type;
        LastFourDigits = lastFourDigits;
        IsDefault = isDefault;
    }
}
