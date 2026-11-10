using EShop.Domain.Aggregates.PaymentAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using EShop.Infrastructure.Payments.Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EShop.API.Controllers;

/// <summary>
/// Stripe payment integration endpoints
/// </summary>
[ApiController]
[Route("api/stripe")]
[Produces("application/json")]
public class StripeController : ControllerBase
{
    private readonly IStripePaymentService _stripeService;
    private readonly IStripeWebhookHandler _webhookHandler;
    private readonly IPaymentRepository _paymentRepository;
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeController> _logger;

    public StripeController(
        IStripePaymentService stripeService,
        IStripeWebhookHandler webhookHandler,
        IPaymentRepository paymentRepository,
        IOptions<StripeSettings> settings,
        ILogger<StripeController> logger)
    {
        _stripeService = stripeService;
        _webhookHandler = webhookHandler;
        _paymentRepository = paymentRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get Stripe publishable key for frontend
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StripeConfigResponse), StatusCodes.Status200OK)]
    public ActionResult<StripeConfigResponse> GetConfig()
    {
        return Ok(new StripeConfigResponse
        {
            PublishableKey = _settings.PublishableKey
        });
    }

    /// <summary>
    /// Create a Payment Intent for card payments
    /// </summary>
    /// <remarks>
    /// Use this for custom payment flows where you handle card collection on the frontend.
    /// Returns a client_secret for use with Stripe.js.
    /// </remarks>
    [HttpPost("payment-intent")]
    [Authorize]
    [ProducesResponseType(typeof(CreatePaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreatePaymentIntent(
        [FromBody] CreatePaymentIntentDto request,
        CancellationToken cancellationToken)
    {
        // Create payment record
        var payment = Payment.Create(
            request.OrderId,
            request.CustomerId,
            new Money(request.Amount, request.Currency),
            PaymentMethodType.CreditCard);

        await _paymentRepository.AddAsync(payment, cancellationToken);

        // Create Stripe PaymentIntent
        var result = await _stripeService.CreatePaymentIntentAsync(new CreatePaymentIntentRequest
        {
            Amount = request.Amount,
            Currency = request.Currency,
            StripeCustomerId = request.StripeCustomerId,
            PaymentMethodId = request.PaymentMethodId,
            OrderId = request.OrderId.ToString(),
            PaymentId = payment.Id.ToString(),
            ReceiptEmail = request.ReceiptEmail,
            SavePaymentMethod = request.SavePaymentMethod,
            AutoConfirm = false, // Let frontend confirm
            Description = $"Order #{request.OrderId}",
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = request.OrderId.ToString(),
                ["customer_id"] = request.CustomerId.ToString()
            }
        }, cancellationToken);

        if (!result.Success)
        {
            payment.Fail(result.ErrorMessage ?? "Failed to create payment intent");
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return BadRequest(new ProblemDetails
            {
                Title = "Payment creation failed",
                Detail = result.ErrorMessage
            });
        }

        // Store gateway transaction ID
        payment.Authorize(result.PaymentIntentId!, null);
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        return Ok(new CreatePaymentIntentResponse
        {
            PaymentId = payment.Id,
            PaymentIntentId = result.PaymentIntentId!,
            ClientSecret = result.ClientSecret!,
            Status = result.Status!,
            RequiresAction = result.RequiresAction,
            NextActionUrl = result.NextActionUrl
        });
    }

    /// <summary>
    /// Create a Stripe Checkout Session
    /// </summary>
    /// <remarks>
    /// Redirects customer to Stripe-hosted payment page.
    /// Best for: Quick integration, mobile-friendly, PCI compliance handled by Stripe.
    /// </remarks>
    [HttpPost("checkout-session")]
    [Authorize]
    [ProducesResponseType(typeof(CreateCheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionDto request,
        CancellationToken cancellationToken)
    {
        // Create payment record
        var totalAmount = request.LineItems?.Sum(li => li.UnitPrice * li.Quantity) ?? request.Amount;
        var payment = Payment.Create(
            request.OrderId,
            request.CustomerId,
            new Money(totalAmount, request.Currency),
            PaymentMethodType.CreditCard);

        await _paymentRepository.AddAsync(payment, cancellationToken);

        // Create Stripe Checkout Session
        var result = await _stripeService.CreateCheckoutSessionAsync(new CreateCheckoutSessionRequest
        {
            Mode = "payment",
            Amount = totalAmount,
            Currency = request.Currency,
            StripeCustomerId = request.StripeCustomerId,
            CustomerEmail = request.CustomerEmail,
            OrderId = request.OrderId.ToString(),
            PaymentId = payment.Id.ToString(),
            Description = request.Description,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            LineItems = request.LineItems?.Select(li => new CheckoutLineItem
            {
                Name = li.Name,
                Description = li.Description,
                UnitPrice = li.UnitPrice,
                Quantity = li.Quantity,
                ImageUrls = li.ImageUrls
            }).ToList(),
            ShippingOptions = request.ShippingOptions?.Select(so => new ShippingOption
            {
                DisplayName = so.DisplayName,
                Amount = so.Amount,
                DeliveryDays = so.DeliveryDays
            }).ToList(),
            CollectShippingAddress = request.CollectShippingAddress,
            AllowedShippingCountries = request.AllowedShippingCountries,
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = request.OrderId.ToString(),
                ["customer_id"] = request.CustomerId.ToString()
            }
        }, cancellationToken);

        if (!result.Success)
        {
            payment.Fail(result.ErrorMessage ?? "Failed to create checkout session");
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return BadRequest(new ProblemDetails
            {
                Title = "Checkout session creation failed",
                Detail = result.ErrorMessage
            });
        }

        return Ok(new CreateCheckoutSessionResponse
        {
            PaymentId = payment.Id,
            SessionId = result.SessionId!,
            Url = result.Url!,
            ExpiresAt = result.ExpiresAt
        });
    }

    /// <summary>
    /// Get Checkout Session status
    /// </summary>
    [HttpGet("checkout-session/{sessionId}")]
    [Authorize]
    [ProducesResponseType(typeof(CheckoutSessionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckoutSessionStatusResponse>> GetCheckoutSessionStatus(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _stripeService.GetCheckoutSessionAsync(sessionId, cancellationToken);

        if (!result.Success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = result.ErrorMessage
            });
        }

        return Ok(new CheckoutSessionStatusResponse
        {
            SessionId = result.SessionId!,
            Status = result.Status!,
            PaymentStatus = result.PaymentStatus!,
            PaymentIntentId = result.PaymentIntentId,
            CustomerEmail = result.CustomerEmail,
            AmountTotal = result.AmountTotal,
            Currency = result.Currency
        });
    }

    /// <summary>
    /// Create a refund for a payment
    /// </summary>
    [HttpPost("refund")]
    [Authorize(Roles = "Admin,Support")]
    [ProducesResponseType(typeof(CreateRefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateRefundResponse>> CreateRefund(
        [FromBody] CreateRefundDto request,
        CancellationToken cancellationToken)
    {
        // Get payment
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment == null)
        {
            return NotFound(new ProblemDetails { Detail = "Payment not found" });
        }

        if (!payment.CanRefund)
        {
            return BadRequest(new ProblemDetails { Detail = "Payment cannot be refunded" });
        }

        if (string.IsNullOrEmpty(payment.GatewayTransactionId))
        {
            return BadRequest(new ProblemDetails { Detail = "No gateway transaction found" });
        }

        // Create domain refund
        var refundAmount = request.Amount.HasValue
            ? new Money(request.Amount.Value, payment.Amount.Currency)
            : payment.AvailableForRefund;

        var domainRefund = payment.RequestRefund(refundAmount, request.Reason, request.RefundType);

        // Process via Stripe
        var result = await _stripeService.CreateRefundAsync(new CreateRefundRequest
        {
            PaymentIntentId = payment.GatewayTransactionId,
            Amount = request.Amount,
            Currency = payment.Amount.Currency,
            Reason = request.RefundType,
            RefundId = domainRefund.Id.ToString()
        }, cancellationToken);

        if (!result.Success)
        {
            payment.RejectRefund(domainRefund.Id, result.ErrorMessage ?? "Stripe refund failed");
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return BadRequest(new ProblemDetails
            {
                Title = "Refund failed",
                Detail = result.ErrorMessage
            });
        }

        // Complete refund
        payment.ProcessRefund(domainRefund.Id, result.RefundId!);
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        _logger.LogInformation("Refund {RefundId} processed for payment {PaymentId}: {Amount}",
            result.RefundId, payment.Id, result.Amount);

        return Ok(new CreateRefundResponse
        {
            RefundId = domainRefund.Id,
            StripeRefundId = result.RefundId!,
            Amount = result.Amount,
            Currency = result.Currency!,
            Status = result.Status!
        });
    }

    /// <summary>
    /// Create or get Stripe customer
    /// </summary>
    [HttpPost("customer")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        [FromBody] CreateStripeCustomerDto request,
        CancellationToken cancellationToken)
    {
        var result = await _stripeService.CreateCustomerAsync(new CreateCustomerRequest
        {
            Email = request.Email,
            Name = request.Name,
            Phone = request.Phone,
            CustomerId = request.CustomerId?.ToString(),
            Address = request.Address != null ? new AddressInfo
            {
                Line1 = request.Address.Line1,
                Line2 = request.Address.Line2,
                City = request.Address.City,
                State = request.Address.State,
                PostalCode = request.Address.PostalCode,
                Country = request.Address.Country
            } : null
        }, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Customer creation failed",
                Detail = result.ErrorMessage
            });
        }

        return Ok(new CustomerResponse
        {
            StripeCustomerId = result.StripeCustomerId!,
            Email = result.Email!,
            Name = result.Name
        });
    }

    /// <summary>
    /// List customer's saved payment methods
    /// </summary>
    [HttpGet("customer/{stripeCustomerId}/payment-methods")]
    [Authorize]
    [ProducesResponseType(typeof(List<PaymentMethodResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentMethodResponse>>> ListPaymentMethods(
        string stripeCustomerId,
        CancellationToken cancellationToken)
    {
        var methods = await _stripeService.ListPaymentMethodsAsync(stripeCustomerId, "card", cancellationToken);

        return Ok(methods.Select(m => new PaymentMethodResponse
        {
            PaymentMethodId = m.PaymentMethodId!,
            Type = m.Type!,
            CardBrand = m.CardBrand,
            CardLast4 = m.CardLast4,
            CardExpMonth = m.CardExpMonth,
            CardExpYear = m.CardExpYear
        }).ToList());
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// </summary>
    /// <remarks>
    /// Configure this URL in your Stripe Dashboard:
    /// https://yourdomain.com/api/stripe/webhook
    /// </remarks>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook received without signature");
            return BadRequest("Missing Stripe-Signature header");
        }

        try
        {
            var stripeEvent = _stripeService.ConstructWebhookEvent(json, signature);
            await _webhookHandler.HandleEventAsync(stripeEvent, cancellationToken);
            return Ok();
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest("Webhook signature verification failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, "Webhook processing failed");
        }
    }
}

#region Request/Response DTOs

public record StripeConfigResponse
{
    public string PublishableKey { get; init; } = string.Empty;
}

public record CreatePaymentIntentDto(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency = "usd",
    string? StripeCustomerId = null,
    string? PaymentMethodId = null,
    string? ReceiptEmail = null,
    bool SavePaymentMethod = false);

public record CreatePaymentIntentResponse
{
    public Guid PaymentId { get; init; }
    public string PaymentIntentId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool RequiresAction { get; init; }
    public string? NextActionUrl { get; init; }
}

public record CreateCheckoutSessionDto(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency = "usd",
    string? StripeCustomerId = null,
    string? CustomerEmail = null,
    string? Description = null,
    string? SuccessUrl = null,
    string? CancelUrl = null,
    List<LineItemDto>? LineItems = null,
    List<ShippingOptionDto>? ShippingOptions = null,
    bool CollectShippingAddress = false,
    List<string>? AllowedShippingCountries = null);

public record LineItemDto(
    string Name,
    decimal UnitPrice,
    int Quantity = 1,
    string? Description = null,
    List<string>? ImageUrls = null);

public record ShippingOptionDto(
    string DisplayName,
    decimal Amount,
    int? DeliveryDays = null);

public record CreateCheckoutSessionResponse
{
    public Guid PaymentId { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}

public record CheckoutSessionStatusResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string? PaymentIntentId { get; init; }
    public string? CustomerEmail { get; init; }
    public long? AmountTotal { get; init; }
    public string? Currency { get; init; }
}

public record CreateRefundDto(
    Guid PaymentId,
    decimal? Amount = null,
    string Reason = "Customer requested refund",
    RefundType RefundType = RefundType.CustomerRequest);

public record CreateRefundResponse
{
    public Guid RefundId { get; init; }
    public string StripeRefundId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public record CreateStripeCustomerDto(
    string Email,
    string? Name = null,
    string? Phone = null,
    Guid? CustomerId = null,
    AddressDto? Address = null);

public record AddressDto(
    string? Line1,
    string? Line2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);

public record CustomerResponse
{
    public string StripeCustomerId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Name { get; init; }
}

public record PaymentMethodResponse
{
    public string PaymentMethodId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? CardBrand { get; init; }
    public string? CardLast4 { get; init; }
    public long? CardExpMonth { get; init; }
    public long? CardExpYear { get; init; }
}

#endregion
