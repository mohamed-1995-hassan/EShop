using EShop.Domain.Aggregates.PaymentAggregate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace EShop.Infrastructure.Payments.Stripe;

/// <summary>
/// Stripe payment gateway service
/// </summary>
public interface IStripePaymentService
{
    // Payment Intents
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentIntentResult> ConfirmPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);
    Task<PaymentIntentResult> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCapture = null, CancellationToken cancellationToken = default);
    Task<PaymentIntentResult> CancelPaymentIntentAsync(string paymentIntentId, string? reason = null, CancellationToken cancellationToken = default);
    Task<PaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    // Checkout Sessions
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request, CancellationToken cancellationToken = default);
    Task<CheckoutSessionResult> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task ExpireCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    // Refunds
    Task<RefundResult> CreateRefundAsync(CreateRefundRequest request, CancellationToken cancellationToken = default);
    Task<RefundResult> GetRefundAsync(string refundId, CancellationToken cancellationToken = default);

    // Customers
    Task<CustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerResult> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    Task<CustomerResult> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    // Payment Methods
    Task<PaymentMethodResult> AttachPaymentMethodAsync(string paymentMethodId, string customerId, CancellationToken cancellationToken = default);
    Task<PaymentMethodResult> DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = default);
    Task<List<PaymentMethodResult>> ListPaymentMethodsAsync(string customerId, string type = "card", CancellationToken cancellationToken = default);

    // Webhooks
    Event ConstructWebhookEvent(string json, string signature);
}

public class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    #region Payment Intents

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = StripeCurrencies.ToStripeAmount(request.Amount, request.Currency),
                Currency = request.Currency.ToLower(),
                Customer = request.StripeCustomerId,
                PaymentMethod = request.PaymentMethodId,
                Description = request.Description,
                Metadata = request.Metadata ?? new Dictionary<string, string>(),
                CaptureMethod = _settings.CaptureMethod == "manual" ? "manual" : "automatic",
                ConfirmationMethod = "automatic",
                SetupFutureUsage = request.SavePaymentMethod ? "off_session" : null,
                ReceiptEmail = request.ReceiptEmail,
                StatementDescriptor = _settings.StatementDescriptor,
            };

            // Add order reference to metadata
            if (!string.IsNullOrEmpty(request.OrderId))
            {
                options.Metadata["order_id"] = request.OrderId;
            }

            if (!string.IsNullOrEmpty(request.PaymentId))
            {
                options.Metadata["payment_id"] = request.PaymentId;
            }

            // Enable 3D Secure if configured
            if (_settings.Enable3DSecure && !string.IsNullOrEmpty(request.PaymentMethodId))
            {
                options.PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
                {
                    Card = new PaymentIntentPaymentMethodOptionsCardOptions
                    {
                        RequestThreeDSecure = "automatic"
                    }
                };
            }

            // Auto-confirm if payment method is provided
            if (!string.IsNullOrEmpty(request.PaymentMethodId))
            {
                options.Confirm = request.AutoConfirm;
                options.ReturnUrl = request.ReturnUrl;
            }

            var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Created PaymentIntent {PaymentIntentId} for amount {Amount} {Currency}",
                intent.Id, request.Amount, request.Currency);

            return MapPaymentIntentResult(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating PaymentIntent: {Message}", ex.Message);
            return PaymentIntentResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<PaymentIntentResult> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var intent = await service.ConfirmAsync(paymentIntentId, cancellationToken: cancellationToken);

            _logger.LogInformation("Confirmed PaymentIntent {PaymentIntentId}, Status: {Status}",
                intent.Id, intent.Status);

            return MapPaymentIntentResult(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error confirming PaymentIntent {PaymentIntentId}: {Message}",
                paymentIntentId, ex.Message);
            return PaymentIntentResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<PaymentIntentResult> CapturePaymentIntentAsync(
        string paymentIntentId,
        long? amountToCapture = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCaptureOptions();

            if (amountToCapture.HasValue)
            {
                options.AmountToCapture = amountToCapture.Value;
            }

            var intent = await service.CaptureAsync(paymentIntentId, options, cancellationToken: cancellationToken);

            _logger.LogInformation("Captured PaymentIntent {PaymentIntentId}", intent.Id);

            return MapPaymentIntentResult(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error capturing PaymentIntent {PaymentIntentId}: {Message}",
                paymentIntentId, ex.Message);
            return PaymentIntentResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<PaymentIntentResult> CancelPaymentIntentAsync(
        string paymentIntentId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCancelOptions
            {
                CancellationReason = reason
            };

            var intent = await service.CancelAsync(paymentIntentId, options, cancellationToken: cancellationToken);

            _logger.LogInformation("Cancelled PaymentIntent {PaymentIntentId}", intent.Id);

            return MapPaymentIntentResult(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error cancelling PaymentIntent {PaymentIntentId}: {Message}",
                paymentIntentId, ex.Message);
            return PaymentIntentResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<PaymentIntentResult> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(paymentIntentId, cancellationToken: cancellationToken);
            return MapPaymentIntentResult(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting PaymentIntent {PaymentIntentId}: {Message}",
                paymentIntentId, ex.Message);
            return PaymentIntentResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    #endregion

    #region Checkout Sessions

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new SessionService();
            var options = new SessionCreateOptions
            {
                Mode = request.Mode,
                Customer = request.StripeCustomerId,
                CustomerEmail = string.IsNullOrEmpty(request.StripeCustomerId) ? request.CustomerEmail : null,
                SuccessUrl = request.SuccessUrl ?? _settings.SuccessUrl,
                CancelUrl = request.CancelUrl ?? _settings.CancelUrl,
                PaymentMethodTypes = _settings.PaymentMethodTypes,
                Metadata = request.Metadata ?? new Dictionary<string, string>(),
                ClientReferenceId = request.OrderId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            };

            // Add order reference
            if (!string.IsNullOrEmpty(request.OrderId))
            {
                options.Metadata["order_id"] = request.OrderId;
            }

            if (!string.IsNullOrEmpty(request.PaymentId))
            {
                options.Metadata["payment_id"] = request.PaymentId;
            }

            // Line items
            if (request.LineItems?.Any() == true)
            {
                options.LineItems = request.LineItems.Select(li => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLower(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = li.Name,
                            Description = li.Description,
                            Images = li.ImageUrls
                        },
                        UnitAmount = StripeCurrencies.ToStripeAmount(li.UnitPrice, request.Currency)
                    },
                    Quantity = li.Quantity
                }).ToList();
            }
            else
            {
                // Single amount
                options.LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.Description ?? "Order Payment"
                            },
                            UnitAmount = StripeCurrencies.ToStripeAmount(request.Amount, request.Currency)
                        },
                        Quantity = 1
                    }
                };
            }

            // Shipping options
            if (request.ShippingOptions?.Any() == true)
            {
                options.ShippingOptions = request.ShippingOptions.Select(so => new SessionShippingOptionOptions
                {
                    ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                    {
                        DisplayName = so.DisplayName,
                        Type = "fixed_amount",
                        FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                        {
                            Amount = StripeCurrencies.ToStripeAmount(so.Amount, request.Currency),
                            Currency = request.Currency.ToLower()
                        },
                        DeliveryEstimate = so.DeliveryDays.HasValue ? new SessionShippingOptionShippingRateDataDeliveryEstimateOptions
                        {
                            Minimum = new SessionShippingOptionShippingRateDataDeliveryEstimateMinimumOptions
                            {
                                Unit = "business_day",
                                Value = so.DeliveryDays.Value
                            },
                            Maximum = new SessionShippingOptionShippingRateDataDeliveryEstimateMaximumOptions
                            {
                                Unit = "business_day",
                                Value = so.DeliveryDays.Value + 2
                            }
                        } : null
                    }
                }).ToList();
            }

            // Subscription mode
            if (request.Mode == "subscription" && !string.IsNullOrEmpty(request.PriceId))
            {
                options.LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = request.PriceId,
                        Quantity = 1
                    }
                };
            }

            // Automatic tax
            if (_settings.AutomaticTax)
            {
                options.AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true };
            }

            // Collect shipping address
            if (request.CollectShippingAddress)
            {
                options.ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = request.AllowedShippingCountries ?? new List<string> { "US", "CA", "GB", "AE", "KW" }
                };
            }

            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Created Checkout Session {SessionId} for order {OrderId}",
                session.Id, request.OrderId);

            return new CheckoutSessionResult
            {
                Success = true,
                SessionId = session.Id,
                Url = session.Url,
                PaymentIntentId = session.PaymentIntentId,
                Status = session.Status,
                ExpiresAt = session.ExpiresAt
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating Checkout Session: {Message}", ex.Message);
            return CheckoutSessionResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<CheckoutSessionResult> GetCheckoutSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, new SessionGetOptions
            {
                Expand = new List<string> { "payment_intent", "customer" }
            }, cancellationToken: cancellationToken);

            return new CheckoutSessionResult
            {
                Success = true,
                SessionId = session.Id,
                Url = session.Url,
                PaymentIntentId = session.PaymentIntentId,
                PaymentStatus = session.PaymentStatus,
                Status = session.Status,
                CustomerId = session.CustomerId,
                CustomerEmail = session.CustomerEmail ?? session.CustomerDetails?.Email,
                AmountTotal = session.AmountTotal,
                Currency = session.Currency,
                Metadata = session.Metadata
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting Checkout Session {SessionId}: {Message}",
                sessionId, ex.Message);
            return CheckoutSessionResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task ExpireCheckoutSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new SessionService();
            await service.ExpireAsync(sessionId, cancellationToken: cancellationToken);

            _logger.LogInformation("Expired Checkout Session {SessionId}", sessionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error expiring Checkout Session {SessionId}: {Message}",
                sessionId, ex.Message);
        }
    }

    #endregion

    #region Refunds

    public async Task<RefundResult> CreateRefundAsync(
        CreateRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Reason = MapRefundReason(request.Reason),
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Partial refund
            if (request.Amount.HasValue)
            {
                options.Amount = StripeCurrencies.ToStripeAmount(request.Amount.Value, request.Currency);
            }

            // Add reference
            if (!string.IsNullOrEmpty(request.RefundId))
            {
                options.Metadata["refund_id"] = request.RefundId;
            }

            var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Created Refund {RefundId} for PaymentIntent {PaymentIntentId}",
                refund.Id, request.PaymentIntentId);

            return new RefundResult
            {
                Success = true,
                RefundId = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                Amount = StripeCurrencies.FromStripeAmount(refund.Amount, refund.Currency),
                Currency = refund.Currency,
                Status = refund.Status,
                Reason = refund.Reason
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating Refund: {Message}", ex.Message);
            return RefundResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<RefundResult> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new RefundService();
            var refund = await service.GetAsync(refundId, cancellationToken: cancellationToken);

            return new RefundResult
            {
                Success = true,
                RefundId = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                Amount = StripeCurrencies.FromStripeAmount(refund.Amount, refund.Currency),
                Currency = refund.Currency,
                Status = refund.Status,
                Reason = refund.Reason
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting Refund {RefundId}: {Message}",
                refundId, ex.Message);
            return RefundResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    #endregion

    #region Customers

    public async Task<CustomerResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new CustomerService();
            var options = new CustomerCreateOptions
            {
                Email = request.Email,
                Name = request.Name,
                Phone = request.Phone,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            if (!string.IsNullOrEmpty(request.CustomerId))
            {
                options.Metadata["customer_id"] = request.CustomerId;
            }

            if (request.Address != null)
            {
                options.Address = new AddressOptions
                {
                    Line1 = request.Address.Line1,
                    Line2 = request.Address.Line2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.Country
                };
            }

            var customer = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Created Stripe Customer {StripeCustomerId} for {Email}",
                customer.Id, request.Email);

            return new CustomerResult
            {
                Success = true,
                StripeCustomerId = customer.Id,
                Email = customer.Email,
                Name = customer.Name
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating Customer: {Message}", ex.Message);
            return CustomerResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<CustomerResult> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new CustomerService();
            var customer = await service.GetAsync(customerId, cancellationToken: cancellationToken);

            return new CustomerResult
            {
                Success = true,
                StripeCustomerId = customer.Id,
                Email = customer.Email,
                Name = customer.Name,
                DefaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting Customer {CustomerId}: {Message}",
                customerId, ex.Message);
            return CustomerResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<CustomerResult> UpdateCustomerAsync(
        string customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new CustomerService();
            var options = new CustomerUpdateOptions();

            if (!string.IsNullOrEmpty(request.Email))
                options.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Name))
                options.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Phone))
                options.Phone = request.Phone;
            if (!string.IsNullOrEmpty(request.DefaultPaymentMethodId))
            {
                options.InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = request.DefaultPaymentMethodId
                };
            }

            var customer = await service.UpdateAsync(customerId, options, cancellationToken: cancellationToken);

            return new CustomerResult
            {
                Success = true,
                StripeCustomerId = customer.Id,
                Email = customer.Email,
                Name = customer.Name
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error updating Customer {CustomerId}: {Message}",
                customerId, ex.Message);
            return CustomerResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    #endregion

    #region Payment Methods

    public async Task<PaymentMethodResult> AttachPaymentMethodAsync(
        string paymentMethodId,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentMethodService();
            var paymentMethod = await service.AttachAsync(paymentMethodId, new PaymentMethodAttachOptions
            {
                Customer = customerId
            }, cancellationToken: cancellationToken);

            return MapPaymentMethodResult(paymentMethod);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error attaching PaymentMethod: {Message}", ex.Message);
            return PaymentMethodResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<PaymentMethodResult> DetachPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentMethodService();
            var paymentMethod = await service.DetachAsync(paymentMethodId, cancellationToken: cancellationToken);

            return MapPaymentMethodResult(paymentMethod);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error detaching PaymentMethod: {Message}", ex.Message);
            return PaymentMethodResult.Failed(ex.StripeError?.Code ?? "unknown", ex.Message);
        }
    }

    public async Task<List<PaymentMethodResult>> ListPaymentMethodsAsync(
        string customerId,
        string type = "card",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentMethodService();
            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = type
            };

            var paymentMethods = await service.ListAsync(options, cancellationToken: cancellationToken);

            return paymentMethods.Data.Select(MapPaymentMethodResult).ToList();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error listing PaymentMethods: {Message}", ex.Message);
            return new List<PaymentMethodResult>();
        }
    }

    #endregion

    #region Webhooks

    public Event ConstructWebhookEvent(string json, string signature)
    {
        return EventUtility.ConstructEvent(json, signature, _settings.WebhookSecret);
    }

    #endregion

    #region Private Helpers

    private static PaymentIntentResult MapPaymentIntentResult(PaymentIntent intent)
    {
        return new PaymentIntentResult
        {
            Success = true,
            PaymentIntentId = intent.Id,
            ClientSecret = intent.ClientSecret,
            Status = intent.Status,
            Amount = StripeCurrencies.FromStripeAmount(intent.Amount, intent.Currency),
            Currency = intent.Currency,
            PaymentMethodId = intent.PaymentMethodId,
            RequiresAction = intent.Status == "requires_action",
            RequiresPaymentMethod = intent.Status == "requires_payment_method",
            NextActionUrl = intent.NextAction?.RedirectToUrl?.Url,
            Metadata = intent.Metadata
        };
    }

    private static PaymentMethodResult MapPaymentMethodResult(PaymentMethod pm)
    {
        return new PaymentMethodResult
        {
            Success = true,
            PaymentMethodId = pm.Id,
            Type = pm.Type,
            CustomerId = pm.CustomerId,
            CardBrand = pm.Card?.Brand,
            CardLast4 = pm.Card?.Last4,
            CardExpMonth = pm.Card?.ExpMonth,
            CardExpYear = pm.Card?.ExpYear,
            CardFingerprint = pm.Card?.Fingerprint
        };
    }

    private static string? MapRefundReason(Domain.Aggregates.PaymentAggregate.RefundType? reason)
    {
        return reason switch
        {
            Domain.Aggregates.PaymentAggregate.RefundType.CustomerRequest => "requested_by_customer",
            Domain.Aggregates.PaymentAggregate.RefundType.DamagedItem or
            Domain.Aggregates.PaymentAggregate.RefundType.WrongItem => "fraudulent",
            Domain.Aggregates.PaymentAggregate.RefundType.OrderCancellation => "requested_by_customer",
            _ => null
        };
    }

    #endregion
}
