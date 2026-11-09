using EShop.Infrastructure.Messaging.Inbox;
using EShop.Infrastructure.Messaging.IntegrationEvents;
using EShop.Infrastructure.Messaging.Sagas;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes ProcessPaymentCommand from the Order Processing Saga.
/// Simulates payment service behavior.
/// 
/// In a real microservices architecture, this would be in a separate Payment Service.
/// </summary>
public class ProcessPaymentCommandConsumer : IConsumer<ProcessPaymentCommand>
{
    private readonly IInboxService _inboxService;
    private readonly ILogger<ProcessPaymentCommandConsumer> _logger;
    private static readonly Random _random = new();

    public ProcessPaymentCommandConsumer(
        IInboxService inboxService,
        ILogger<ProcessPaymentCommandConsumer> logger)
    {
        _inboxService = inboxService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
    {
        var command = context.Message;

        // Deterministic message ID for idempotency
        var messageId = new Guid(System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes($"payment:{command.OrderId}:{command.Amount}")));

        var wasProcessed = await _inboxService.TryProcessAsync(
            messageId,
            nameof(ProcessPaymentCommand),
            nameof(ProcessPaymentCommandConsumer),
            async () => await ProcessPayment(context, command),
            command.CorrelationId,
            command.ConversationId,
            context.CancellationToken);

        if (!wasProcessed)
        {
            _logger.LogDebug("ProcessPayment command for Order {OrderId} was already processed", command.OrderId);
        }
    }

    private async Task ProcessPayment(ConsumeContext<ProcessPaymentCommand> context, ProcessPaymentCommand command)
    {
        _logger.LogInformation(
            "Processing payment for Order {OrderId}, Amount: {Amount} {Currency}",
            command.OrderId,
            command.Amount,
            command.Currency);

        // Simulate payment processing delay
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // Simulate occasional payment failures (10% chance)
        var shouldFail = _random.NextDouble() < 0.1;

        if (shouldFail)
        {
            _logger.LogWarning("Payment failed for Order {OrderId}", command.OrderId);

            await context.Publish(new PaymentFailedIntegrationEvent
            {
                OrderId = command.OrderId,
                Reason = "Insufficient funds",
                ErrorCode = "INSUFFICIENT_FUNDS",
                CorrelationId = command.CorrelationId,
                ConversationId = command.ConversationId
            });
        }
        else
        {
            var paymentId = Guid.NewGuid();
            var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{paymentId.ToString()[..8]}";

            _logger.LogInformation(
                "Payment completed for Order {OrderId}, PaymentId: {PaymentId}, TransactionId: {TransactionId}",
                command.OrderId,
                paymentId,
                transactionId);

            await context.Publish(new PaymentCompletedIntegrationEvent
            {
                OrderId = command.OrderId,
                PaymentId = paymentId,
                Amount = command.Amount,
                Currency = command.Currency,
                PaymentMethod = "CreditCard",
                TransactionId = transactionId,
                CorrelationId = command.CorrelationId,
                ConversationId = command.ConversationId
            });
        }
    }
}

/// <summary>
/// Consumer definition with retry and circuit breaker
/// </summary>
public class ProcessPaymentCommandConsumerDefinition : ConsumerDefinition<ProcessPaymentCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ProcessPaymentCommandConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Payment processing - retry with backoff
        endpointConfigurator.UseMessageRetry(r => r
            .Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(10)));

        // Circuit breaker for payment gateway protection
        endpointConfigurator.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 20;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });
    }
}
