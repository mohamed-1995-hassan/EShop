using EShop.Infrastructure.Messaging.Inbox;
using EShop.Infrastructure.Messaging.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes OrderPlacedIntegrationEvent.
/// 
/// CONSUMER RESPONSIBILITIES:
/// 1. Use Inbox pattern for idempotent processing
/// 2. Trigger downstream actions (analytics, notifications, etc.)
/// 3. Handle errors gracefully
/// 
/// NOTE: The Saga handles the main order workflow (stock + payment).
/// This consumer is for additional side effects.
/// </summary>
public class OrderPlacedConsumer : IConsumer<OrderPlacedIntegrationEvent>
{
    private readonly IInboxService _inboxService;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(
        IInboxService inboxService,
        ILogger<OrderPlacedConsumer> logger)
    {
        _inboxService = inboxService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        var message = context.Message;
        
        var wasProcessed = await _inboxService.TryProcessAsync(
            message.EventId,
            nameof(OrderPlacedIntegrationEvent),
            nameof(OrderPlacedConsumer),
            async () =>
            {
                _logger.LogInformation(
                    "Processing OrderPlaced event for Order {OrderId}, Customer {CustomerId}, Amount {Amount} {Currency}",
                    message.OrderId,
                    message.CustomerId,
                    message.TotalAmount,
                    message.Currency);

                // Side effects:
                // - Send confirmation email to customer
                // - Update analytics/reporting
                // - Trigger marketing automation
                await Task.CompletedTask;
            },
            message.CorrelationId,
            message.ConversationId,
            context.CancellationToken);

        if (!wasProcessed)
        {
            _logger.LogDebug("OrderPlaced event {EventId} was already processed, skipping", message.EventId);
        }
    }
}

/// <summary>
/// Consumer definition with retry and DLQ configuration
/// </summary>
public class OrderPlacedConsumerDefinition : ConsumerDefinition<OrderPlacedConsumer>
{
    public OrderPlacedConsumerDefinition()
    {
        // Prefetch count - how many messages to load at once
        ConcurrentMessageLimit = 10;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderPlacedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Retry policy with exponential backoff
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

        // Circuit breaker to prevent cascading failures
        endpointConfigurator.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });
    }
}
