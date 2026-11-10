using EShop.Domain.Aggregates.PaymentAggregate;
using EShop.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace EShop.Infrastructure.Payments.Stripe;

/// <summary>
/// Handles Stripe webhook events
/// </summary>
public interface IStripeWebhookHandler
{
    Task HandleEventAsync(Event stripeEvent, CancellationToken cancellationToken = default);
}

public class StripeWebhookHandler : IStripeWebhookHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<StripeWebhookHandler> _logger;

    public StripeWebhookHandler(
        IPaymentRepository paymentRepository,
        ILogger<StripeWebhookHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Stripe webhook: {EventType} ({EventId})",
            stripeEvent.Type, stripeEvent.Id);

        try
        {
            switch (stripeEvent.Type)
            {
                case StripeWebhookEvents.PaymentIntentSucceeded:
                    await HandlePaymentIntentSucceededAsync(stripeEvent, cancellationToken);
                    break;

                case StripeWebhookEvents.PaymentIntentPaymentFailed:
                    await HandlePaymentIntentFailedAsync(stripeEvent, cancellationToken);
                    break;

                case StripeWebhookEvents.PaymentIntentCanceled:
                    await HandlePaymentIntentCanceledAsync(stripeEvent, cancellationToken);
                    break;

                case StripeWebhookEvents.CheckoutSessionCompleted:
                    await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                    break;

                case StripeWebhookEvents.CheckoutSessionExpired:
                    await HandleCheckoutSessionExpiredAsync(stripeEvent, cancellationToken);
                    break;

                case StripeWebhookEvents.ChargeRefunded:
                    await HandleChargeRefundedAsync(stripeEvent, cancellationToken);
                    break;

                case StripeWebhookEvents.ChargeDisputed:
                    await HandleChargeDisputedAsync(stripeEvent, cancellationToken);
                    break;

                default:
                    _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook {EventType}: {Message}",
                stripeEvent.Type, ex.Message);
            throw;
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var paymentId = paymentIntent.Metadata.TryGetValue("payment_id", out var pid) ? pid : null;
        
        Payment? payment = null;

        if (!string.IsNullOrEmpty(paymentId) && Guid.TryParse(paymentId, out var id))
        {
            payment = await _paymentRepository.GetByIdAsync(id, cancellationToken);
        }

        payment ??= await _paymentRepository.GetByGatewayTransactionIdAsync(paymentIntent.Id, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for PaymentIntent {PaymentIntentId}", paymentIntent.Id);
            return;
        }

        if (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Failed)
        {
            payment.AuthorizeAndCapture(
                paymentIntent.Id,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    paymentIntent.Status,
                    paymentIntent.PaymentMethodId,
                    paymentIntent.LatestChargeId
                }));

            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            _logger.LogInformation("Payment {PaymentId} captured via PaymentIntent {PaymentIntentId}",
                payment.Id, paymentIntent.Id);
        }
    }

    private async Task HandlePaymentIntentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var paymentId = paymentIntent.Metadata.TryGetValue("payment_id", out var pid) ? pid : null;

        Payment? payment = null;

        if (!string.IsNullOrEmpty(paymentId) && Guid.TryParse(paymentId, out var id))
        {
            payment = await _paymentRepository.GetByIdAsync(id, cancellationToken);
        }

        payment ??= await _paymentRepository.GetByGatewayTransactionIdAsync(paymentIntent.Id, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for failed PaymentIntent {PaymentIntentId}", paymentIntent.Id);
            return;
        }

        var failureMessage = paymentIntent.LastPaymentError?.Message ?? "Payment failed";
        payment.Fail(failureMessage);

        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        _logger.LogWarning("Payment {PaymentId} failed via PaymentIntent {PaymentIntentId}: {Reason}",
            payment.Id, paymentIntent.Id, failureMessage);
    }

    private async Task HandlePaymentIntentCanceledAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var paymentId = paymentIntent.Metadata.TryGetValue("payment_id", out var pid) ? pid : null;

        Payment? payment = null;

        if (!string.IsNullOrEmpty(paymentId) && Guid.TryParse(paymentId, out var id))
        {
            payment = await _paymentRepository.GetByIdAsync(id, cancellationToken);
        }

        payment ??= await _paymentRepository.GetByGatewayTransactionIdAsync(paymentIntent.Id, cancellationToken);

        if (payment == null) return;

        if (payment.Status == PaymentStatus.Authorized)
        {
            payment.Void(paymentIntent.CancellationReason ?? "Cancelled via Stripe");
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        _logger.LogInformation("Checkout session completed: {SessionId}, PaymentStatus: {PaymentStatus}",
            session.Id, session.PaymentStatus);

        // Payment status is "paid" for successful payments
        if (session.PaymentStatus != "paid") return;

        var paymentId = session.Metadata.TryGetValue("payment_id", out var pid) ? pid : null;

        Payment? payment = null;

        if (!string.IsNullOrEmpty(paymentId) && Guid.TryParse(paymentId, out var id))
        {
            payment = await _paymentRepository.GetByIdAsync(id, cancellationToken);
        }

        if (!string.IsNullOrEmpty(session.PaymentIntentId))
        {
            payment ??= await _paymentRepository.GetByGatewayTransactionIdAsync(session.PaymentIntentId, cancellationToken);
        }

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for Checkout Session {SessionId}", session.Id);
            return;
        }

        if (payment.Status == PaymentStatus.Pending)
        {
            payment.AuthorizeAndCapture(
                session.PaymentIntentId ?? session.Id,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    session.PaymentStatus,
                    session.PaymentIntentId,
                    session.CustomerId
                }));

            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            _logger.LogInformation("Payment {PaymentId} captured via Checkout Session {SessionId}",
                payment.Id, session.Id);
        }
    }

    private async Task HandleCheckoutSessionExpiredAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        _logger.LogWarning("Checkout session expired: {SessionId}", session.Id);

        // Optionally update payment status to expired
        var paymentId = session.Metadata.TryGetValue("payment_id", out var pid) ? pid : null;

        if (!string.IsNullOrEmpty(paymentId) && Guid.TryParse(paymentId, out var id))
        {
            var payment = await _paymentRepository.GetByIdAsync(id, cancellationToken);
            if (payment != null && payment.Status == PaymentStatus.Pending)
            {
                payment.Fail("Checkout session expired");
                await _paymentRepository.UpdateAsync(payment, cancellationToken);
            }
        }
    }

    private async Task HandleChargeRefundedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return;

        _logger.LogInformation("Charge refunded: {ChargeId}, Refunded: {AmountRefunded}",
            charge.Id, charge.AmountRefunded);

        // Find payment by charge's payment intent
        if (string.IsNullOrEmpty(charge.PaymentIntentId)) return;

        var payment = await _paymentRepository.GetByGatewayTransactionIdAsync(charge.PaymentIntentId, cancellationToken);
        if (payment == null) return;

        // Refund processing is typically handled through our API
        // This is just for logging/monitoring external refunds
        _logger.LogInformation("Refund detected for Payment {PaymentId}: {Amount}",
            payment.Id, charge.AmountRefunded / 100m);
    }

    private async Task HandleChargeDisputedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var dispute = stripeEvent.Data.Object as Dispute;
        if (dispute == null) return;

        _logger.LogWarning("Charge disputed: {DisputeId}, Amount: {Amount}, Reason: {Reason}",
            dispute.Id, dispute.Amount / 100m, dispute.Reason);

        // Find payment by charge's payment intent
        if (dispute.PaymentIntent == null) return;

        var payment = await _paymentRepository.GetByGatewayTransactionIdAsync(dispute.PaymentIntentId, cancellationToken);
        if (payment == null) return;

        // Log dispute - actual handling depends on business requirements
        _logger.LogWarning("Dispute opened for Payment {PaymentId}: {Reason}",
            payment.Id, dispute.Reason);

        // You might want to:
        // 1. Send notification to admin
        // 2. Create a support ticket
        // 3. Update order status
        // 4. Request refund if valid dispute
    }
}
