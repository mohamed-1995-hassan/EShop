using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.PaymentAggregate;

/// <summary>
/// Payment Aggregate Root - Handles payment processing and refunds
/// 
/// BUSINESS RULES:
/// - Payment amount must match order total
/// - Refunds cannot exceed paid amount
/// - Partial refunds allowed
/// - Payment gateway transactions tracked
/// - Retry logic for failed payments
/// </summary>
public class Payment : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Money RefundedAmount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public PaymentMethodType PaymentMethod { get; private set; }
    public string? PaymentMethodDetails { get; private set; }
    public string? GatewayTransactionId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public string? FailureReason { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private readonly List<PaymentTransaction> _transactions = new();
    public IReadOnlyCollection<PaymentTransaction> Transactions => _transactions.AsReadOnly();

    private readonly List<Refund> _refunds = new();
    public IReadOnlyCollection<Refund> Refunds => _refunds.AsReadOnly();

    public Money AvailableForRefund => Amount.Subtract(RefundedAmount);
    public bool CanRefund => Status == PaymentStatus.Captured && RefundedAmount.Amount < Amount.Amount;

    private Payment() { }

    public static Payment Create(
        Guid orderId,
        Guid customerId,
        Money amount,
        PaymentMethodType paymentMethod,
        string? paymentMethodDetails = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required");
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required");
        if (amount.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero");

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            RefundedAmount = Money.Zero(amount.Currency),
            Status = PaymentStatus.Pending,
            PaymentMethod = paymentMethod,
            PaymentMethodDetails = paymentMethodDetails,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Payment Processing

    public void Authorize(string gatewayTransactionId, string? gatewayResponse = null)
    {
        if (Status != PaymentStatus.Pending && Status != PaymentStatus.Failed)
            throw new DomainException("Payment cannot be authorized in current status");

        AttemptCount++;
        GatewayTransactionId = gatewayTransactionId;
        GatewayResponse = gatewayResponse;
        Status = PaymentStatus.Authorized;

        _transactions.Add(PaymentTransaction.Create(
            TransactionType.Authorization,
            Amount,
            gatewayTransactionId,
            "Payment authorized"));
    }

    public void Capture(string? captureTransactionId = null)
    {
        if (Status != PaymentStatus.Authorized)
            throw new DomainException("Payment must be authorized before capture");

        Status = PaymentStatus.Captured;
        ProcessedAt = DateTime.UtcNow;

        _transactions.Add(PaymentTransaction.Create(
            TransactionType.Capture,
            Amount,
            captureTransactionId ?? GatewayTransactionId!,
            "Payment captured"));
    }

    public void AuthorizeAndCapture(string gatewayTransactionId, string? gatewayResponse = null)
    {
        Authorize(gatewayTransactionId, gatewayResponse);
        Capture(gatewayTransactionId);
    }

    public void Fail(string reason)
    {
        AttemptCount++;
        FailureReason = reason;
        Status = PaymentStatus.Failed;

        _transactions.Add(PaymentTransaction.Create(
            TransactionType.Failure,
            Amount,
            GatewayTransactionId ?? "N/A",
            $"Payment failed: {reason}"));
    }

    public void Void(string reason)
    {
        if (Status != PaymentStatus.Authorized)
            throw new DomainException("Only authorized payments can be voided");

        Status = PaymentStatus.Voided;

        _transactions.Add(PaymentTransaction.Create(
            TransactionType.Void,
            Amount,
            GatewayTransactionId!,
            $"Payment voided: {reason}"));
    }

    public bool CanRetry()
    {
        return Status == PaymentStatus.Failed && AttemptCount < 3;
    }

    #endregion

    #region Refunds

    public Refund RequestRefund(Money refundAmount, string reason, RefundType type = RefundType.CustomerRequest)
    {
        if (!CanRefund)
            throw new DomainException("Payment cannot be refunded");

        if (refundAmount.Amount > AvailableForRefund.Amount)
            throw new DomainException($"Refund amount exceeds available amount ({AvailableForRefund})");

        var refund = Refund.Create(Id, refundAmount, reason, type);
        _refunds.Add(refund);

        return refund;
    }

    public void ProcessRefund(Guid refundId, string gatewayRefundId)
    {
        var refund = _refunds.FirstOrDefault(r => r.Id == refundId)
            ?? throw new DomainException("Refund not found");

        refund.Process(gatewayRefundId);
        RefundedAmount = RefundedAmount.Add(refund.Amount);

        _transactions.Add(PaymentTransaction.Create(
            TransactionType.Refund,
            refund.Amount,
            gatewayRefundId,
            $"Refund processed: {refund.Reason}"));

        // Update status if fully refunded
        if (RefundedAmount.Amount >= Amount.Amount)
        {
            Status = PaymentStatus.Refunded;
        }
        else
        {
            Status = PaymentStatus.PartiallyRefunded;
        }
    }

    public void RejectRefund(Guid refundId, string reason)
    {
        var refund = _refunds.FirstOrDefault(r => r.Id == refundId)
            ?? throw new DomainException("Refund not found");

        refund.Reject(reason);
    }

    #endregion
}

public class PaymentTransaction : Entity<Guid>
{
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string GatewayTransactionId { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentTransaction() { } // EF Core

    internal static PaymentTransaction Create(
        TransactionType type,
        Money amount,
        string gatewayTransactionId,
        string? description = null)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            GatewayTransactionId = gatewayTransactionId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class Refund : Entity<Guid>
{
    public Guid PaymentId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string Reason { get; private set; } = string.Empty;
    public RefundType Type { get; private set; }
    public RefundStatus Status { get; private set; }
    public string? GatewayRefundId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Refund() { } // EF Core

    internal static Refund Create(Guid paymentId, Money amount, string reason, RefundType type)
    {
        return new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Amount = amount,
            Reason = reason,
            Type = type,
            Status = RefundStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
    }

    internal void Process(string gatewayRefundId)
    {
        if (Status != RefundStatus.Pending)
            throw new DomainException("Refund already processed");

        GatewayRefundId = gatewayRefundId;
        Status = RefundStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    internal void Reject(string reason)
    {
        if (Status != RefundStatus.Pending)
            throw new DomainException("Refund already processed");

        RejectionReason = reason;
        Status = RefundStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}

public enum PaymentStatus
{
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Failed = 4,
    Voided = 5,
    Refunded = 6,
    PartiallyRefunded = 7,
    Expired = 8
}

public enum PaymentMethodType
{
    CreditCard = 1,
    DebitCard = 2,
    BankTransfer = 3,
    Wallet = 4,
    CashOnDelivery = 5,
    Knet = 6,
    ApplePay = 7,
    GooglePay = 8,
    PayPal = 9
}

public enum TransactionType
{
    Authorization = 1,
    Capture = 2,
    Refund = 3,
    Void = 4,
    Failure = 5
}

public enum RefundStatus
{
    Pending = 1,
    Completed = 2,
    Rejected = 3,
    Failed = 4
}

public enum RefundType
{
    CustomerRequest = 1,
    OrderCancellation = 2,
    DamagedItem = 3,
    WrongItem = 4,
    PriceAdjustment = 5,
    Dispute = 6
}
