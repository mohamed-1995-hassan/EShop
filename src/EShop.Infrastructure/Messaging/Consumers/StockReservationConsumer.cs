using EShop.Domain.Repositories;
using EShop.Infrastructure.Messaging.Inbox;
using EShop.Infrastructure.Messaging.IntegrationEvents;
using EShop.Infrastructure.Messaging.Sagas;
using EShop.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes ReserveStockCommand from the Order Processing Saga.
/// Simulates inventory service behavior.
/// 
/// In a real microservices architecture, this would be in a separate Inventory Service.
/// </summary>
public class ReserveStockCommandConsumer : IConsumer<ReserveStockCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly EShopDbContext _dbContext;
    private readonly IInboxService _inboxService;
    private readonly ILogger<ReserveStockCommandConsumer> _logger;

    public ReserveStockCommandConsumer(
        IProductRepository productRepository,
        EShopDbContext dbContext,
        IInboxService inboxService,
        ILogger<ReserveStockCommandConsumer> logger)
    {
        _productRepository = productRepository;
        _dbContext = dbContext;
        _inboxService = inboxService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var command = context.Message;

        // Use a hash of command for idempotency
        var messageId = GenerateMessageId(command);

        var wasProcessed = await _inboxService.TryProcessAsync(
            messageId,
            nameof(ReserveStockCommand),
            nameof(ReserveStockCommandConsumer),
            async () => await ProcessReservation(context, command),
            command.CorrelationId,
            command.ConversationId,
            context.CancellationToken);

        if (!wasProcessed)
        {
            _logger.LogDebug("ReserveStock command for Order {OrderId} was already processed", command.OrderId);
        }
    }

    private async Task ProcessReservation(ConsumeContext<ReserveStockCommand> context, ReserveStockCommand command)
    {
        _logger.LogInformation("Reserving stock for Order {OrderId}", command.OrderId);

        var failedItems = new List<FailedItemDto>();
        var reservedItems = new List<ReservedItemDto>();

        foreach (var item in command.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            
            if (product == null)
            {
                failedItems.Add(new FailedItemDto
                {
                    ProductId = item.ProductId,
                    Sku = "UNKNOWN",
                    RequestedQuantity = item.Quantity,
                    AvailableQuantity = 0
                });
                continue;
            }

            if (!product.CanReserve(item.Quantity))
            {
                failedItems.Add(new FailedItemDto
                {
                    ProductId = item.ProductId,
                    Sku = product.Sku,
                    RequestedQuantity = item.Quantity,
                    AvailableQuantity = product.StockQuantity
                });
                continue;
            }

            // Reserve the stock
            product.Reserve(item.Quantity);
            reservedItems.Add(new ReservedItemDto
            {
                ProductId = item.ProductId,
                Sku = product.Sku,
                QuantityReserved = item.Quantity
            });
        }

        // Save changes
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        // Publish result
        if (failedItems.Any())
        {
            _logger.LogWarning("Stock reservation failed for Order {OrderId}. Failed items: {Count}", 
                command.OrderId, failedItems.Count);

            await context.Publish(new StockReservationFailedIntegrationEvent
            {
                OrderId = command.OrderId,
                FailedItems = failedItems,
                Reason = $"Insufficient stock for {failedItems.Count} item(s)",
                CorrelationId = command.CorrelationId,
                ConversationId = command.ConversationId
            });
        }
        else
        {
            _logger.LogInformation("Stock reserved successfully for Order {OrderId}", command.OrderId);

            await context.Publish(new StockReservedIntegrationEvent
            {
                OrderId = command.OrderId,
                ReservedItems = reservedItems,
                CorrelationId = command.CorrelationId,
                ConversationId = command.ConversationId
            });
        }
    }

    private static Guid GenerateMessageId(ReserveStockCommand command)
    {
        // Create deterministic ID from command content for idempotency
        var content = $"{command.OrderId}:{string.Join(",", command.Items.Select(i => $"{i.ProductId}:{i.Quantity}"))}";
        return new Guid(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(content)));
    }
}

/// <summary>
/// Consumes ReleaseStockCommand for compensation.
/// </summary>
public class ReleaseStockCommandConsumer : IConsumer<ReleaseStockCommand>
{
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<ReleaseStockCommandConsumer> _logger;

    public ReleaseStockCommandConsumer(
        EShopDbContext dbContext,
        ILogger<ReleaseStockCommandConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseStockCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Releasing stock for cancelled Order {OrderId}", command.OrderId);

        // In a real system, you would:
        // 1. Look up the reservation
        // 2. Release the reserved stock back to available
        // 3. Publish StockReleasedIntegrationEvent

        // For demo, just publish success
        await context.Publish(new StockReleasedIntegrationEvent
        {
            OrderId = command.OrderId,
            ReleasedItems = new List<ReleasedItemDto>(), // Would contain actual items
            CorrelationId = command.CorrelationId
        });
    }
}

/// <summary>
/// Consumer definitions with retry policies
/// </summary>
public class ReserveStockCommandConsumerDefinition : ConsumerDefinition<ReserveStockCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ReserveStockCommandConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Stock reservation is critical - retry with longer intervals
        endpointConfigurator.UseMessageRetry(r => r
            .Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5)));
    }
}
