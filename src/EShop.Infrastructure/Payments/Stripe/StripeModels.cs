using EShop.Domain.Aggregates.PaymentAggregate;

namespace EShop.Infrastructure.Payments.Stripe;

#region Payment Intent

public class CreatePaymentIntentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? StripeCustomerId { get; set; }
    public string? PaymentMethodId { get; set; }
    public string? Description { get; set; }
    public string? OrderId { get; set; }
    public string? PaymentId { get; set; }
    public string? ReceiptEmail { get; set; }
    public bool SavePaymentMethod { get; set; }
    public bool AutoConfirm { get; set; } = true;
    public string? ReturnUrl { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class PaymentIntentResult
{
    public bool Success { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? PaymentMethodId { get; set; }
    public bool RequiresAction { get; set; }
    public bool RequiresPaymentMethod { get; set; }
    public string? NextActionUrl { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }

    public static PaymentIntentResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

#endregion

#region Checkout Session

public class CreateCheckoutSessionRequest
{
    public string Mode { get; set; } = "payment"; // "payment", "subscription", "setup"
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? StripeCustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? OrderId { get; set; }
    public string? PaymentId { get; set; }
    public string? Description { get; set; }
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? PriceId { get; set; } // For subscriptions
    public List<CheckoutLineItem>? LineItems { get; set; }
    public List<ShippingOption>? ShippingOptions { get; set; }
    public bool CollectShippingAddress { get; set; }
    public List<string>? AllowedShippingCountries { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CheckoutLineItem
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public List<string>? ImageUrls { get; set; }
}

public class ShippingOption
{
    public string DisplayName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? DeliveryDays { get; set; }
}

public class CheckoutSessionResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? Url { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? PaymentStatus { get; set; }
    public string? Status { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public long? AmountTotal { get; set; }
    public string? Currency { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }

    public static CheckoutSessionResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

#endregion

#region Refund

public class CreateRefundRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal? Amount { get; set; } // Null for full refund
    public string Currency { get; set; } = "usd";
    public RefundType? Reason { get; set; }
    public string? RefundId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class RefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string? PaymentIntentId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? Reason { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static RefundResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

#endregion

#region Customer

public class CreateCustomerRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? CustomerId { get; set; } // Your internal customer ID
    public AddressInfo? Address { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class UpdateCustomerRequest
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? DefaultPaymentMethodId { get; set; }
}

public class AddressInfo
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

public class CustomerResult
{
    public bool Success { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? DefaultPaymentMethodId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static CustomerResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

#endregion

#region Payment Method

public class PaymentMethodResult
{
    public bool Success { get; set; }
    public string? PaymentMethodId { get; set; }
    public string? Type { get; set; }
    public string? CustomerId { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLast4 { get; set; }
    public long? CardExpMonth { get; set; }
    public long? CardExpYear { get; set; }
    public string? CardFingerprint { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static PaymentMethodResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

#endregion

#region Webhook Events

/// <summary>
/// Stripe webhook event types we handle
/// </summary>
public static class StripeWebhookEvents
{
    // Payment Intent events
    public const string PaymentIntentSucceeded = "payment_intent.succeeded";
    public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    public const string PaymentIntentCanceled = "payment_intent.canceled";
    public const string PaymentIntentRequiresAction = "payment_intent.requires_action";
    public const string PaymentIntentAmountCapturableUpdated = "payment_intent.amount_capturable_updated";

    // Checkout Session events
    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string CheckoutSessionExpired = "checkout.session.expired";
    public const string CheckoutSessionAsyncPaymentSucceeded = "checkout.session.async_payment_succeeded";
    public const string CheckoutSessionAsyncPaymentFailed = "checkout.session.async_payment_failed";

    // Charge events
    public const string ChargeSucceeded = "charge.succeeded";
    public const string ChargeFailed = "charge.failed";
    public const string ChargeRefunded = "charge.refunded";
    public const string ChargeDisputed = "charge.dispute.created";

    // Refund events
    public const string RefundCreated = "refund.created";
    public const string RefundUpdated = "refund.updated";

    // Customer events
    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";
    public const string CustomerDeleted = "customer.deleted";

    // Subscription events (for future use)
    public const string CustomerSubscriptionCreated = "customer.subscription.created";
    public const string CustomerSubscriptionUpdated = "customer.subscription.updated";
    public const string CustomerSubscriptionDeleted = "customer.subscription.deleted";
    public const string InvoicePaid = "invoice.paid";
    public const string InvoicePaymentFailed = "invoice.payment_failed";
}

#endregion
