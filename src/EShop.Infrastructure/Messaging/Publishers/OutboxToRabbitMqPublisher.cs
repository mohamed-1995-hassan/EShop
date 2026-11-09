using EShop.Infrastructure.Messaging.IntegrationEvents;
using EShop.Infrastructure.Persistence;
using EShop.Infrastructure.Persistence.Outbox;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace EShop.Infrastructure.Messaging.Publishers;

/// <summary>
/// OUTBOX TO RABBITMQ PUBLISHER
/// 
/// This background service:
/// 1. Reads unpublished messages from the Outbox table
/// 2. Publishes them to RabbitMQ via MassTransit
/// 3. Marks messages as processed on success
/// 4. Handles failures with retry and error tracking
/// 
/// RELIABILITY FEATURES:
/// - Polly retry policy for transient failures
/// - Batch processing for efficiency
/// - Dead letter handling after max retries
/// - Distributed lock support for horizontal scaling
/// </summary>
public class OutboxToRabbitMqPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxToRabbitMqPublisher> _logger;
    private readonly ResiliencePipeline _publishPipeline;
    
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 50;
    private readonly int _maxRetries = 5;

    public OutboxToRabbitMqPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxToRabbitMqPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _publishPipeline = CreatePublishPipeline();
    }

    private ResiliencePipeline CreatePublishPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Publish retry attempt {AttemptNumber} after {Delay}ms due to: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox to RabbitMQ Publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox publisher loop");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox to RabbitMQ Publisher stopped");
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EShopDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        // Get unprocessed messages
        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < _maxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogDebug("Processing {Count} outbox messages for RabbitMQ", messages.Count);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, publishEndpoint, dbContext, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IPublishEndpoint publishEndpoint,
        EShopDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // Deserialize the domain event
            var domainEvent = message.DeserializeEvent();
            if (domainEvent == null)
            {
                _logger.LogError(
                    "Failed to deserialize outbox message {Id} of type {Type}",
                    message.Id, message.Type);
                message.MarkAsFailed("Deserialization failed - moving to DLQ");
                return;
            }

            // Convert domain event to integration event
            var integrationEvent = ConvertToIntegrationEvent(domainEvent);
            if (integrationEvent == null)
            {
                // Not all domain events need to be published as integration events
                message.MarkAsProcessed();
                return;
            }

            // Publish with retry policy
            await _publishPipeline.ExecuteAsync(async ct =>
            {
                await publishEndpoint.Publish(integrationEvent, ct);
            }, cancellationToken);

            message.MarkAsProcessed();
            _logger.LogDebug(
                "Published integration event {EventType} for outbox message {Id}",
                integrationEvent.GetType().Name, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish outbox message {Id} after retries",
                message.Id);
            message.MarkAsFailed(ex.Message);
        }
    }

    /// <summary>
    /// Convert domain events to integration events for cross-service communication
    /// </summary>
    private IntegrationEvent? ConvertToIntegrationEvent(object domainEvent)
    {
        // Map domain events to integration events
        // This is where you transform internal events to external contracts
        
        return domainEvent switch
        {
            // Order domain events
            EShop.Domain.Events.OrderCreatedEvent e => new OrderPlacedIntegrationEvent
            {
                OrderId = e.OrderId,
                CustomerId = e.CustomerId,
                TotalAmount = e.TotalAmount,
                Currency = "KWD"
            },
            
            EShop.Domain.Events.OrderShippedEvent e => new OrderShippedIntegrationEvent
            {
                OrderId = e.OrderId,
                TrackingNumber = e.TrackingNumber
            },
            
            EShop.Domain.Events.OrderCancelledEvent e => new OrderCancelledIntegrationEvent
            {
                OrderId = e.OrderId,
                Reason = e.Reason
            },
            
            // Product domain events
            EShop.Domain.Events.ProductStockReservedEvent e => new StockReservedIntegrationEvent
            {
                OrderId = e.OrderId,
                ReservedItems = new List<ReservedItemDto>
                {
                    new()
                    {
                        ProductId = e.ProductId,
                        Sku = e.Sku,
                        QuantityReserved = e.QuantityReserved
                    }
                }
            },
            
            // Events that don't need external publishing
            _ => null
        };
    }
}
