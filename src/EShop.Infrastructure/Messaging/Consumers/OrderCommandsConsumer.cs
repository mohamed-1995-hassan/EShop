using EShop.Domain.Repositories;
using EShop.Infrastructure.Messaging.Sagas;
using EShop.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes ConfirmOrderCommand from the saga.
/// Marks the order as confirmed after successful payment.
/// </summary>
public class ConfirmOrderCommandConsumer : IConsumer<ConfirmOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<ConfirmOrderCommandConsumer> _logger;

    public ConfirmOrderCommandConsumer(
        IOrderRepository orderRepository,
        EShopDbContext dbContext,
        ILogger<ConfirmOrderCommandConsumer> logger)
    {
        _orderRepository = orderRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConfirmOrderCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Confirming Order {OrderId}", command.OrderId);

        var order = await _orderRepository.GetByIdAsync(command.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for confirmation", command.OrderId);
            return;
        }

        // Confirm the order (domain method)
        order.Confirm();

        await _dbContext.SaveChangesWithOutboxAsync(context.CancellationToken);

        _logger.LogInformation("Order {OrderId} confirmed successfully", command.OrderId);
    }
}

/// <summary>
/// Consumes CancelOrderCommand from the saga.
/// Cancels the order when stock reservation or payment fails.
/// </summary>
public class CancelOrderCommandConsumer : IConsumer<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<CancelOrderCommandConsumer> _logger;

    public CancelOrderCommandConsumer(
        IOrderRepository orderRepository,
        EShopDbContext dbContext,
        ILogger<CancelOrderCommandConsumer> logger)
    {
        _orderRepository = orderRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CancelOrderCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Cancelling Order {OrderId}, Reason: {Reason}", 
            command.OrderId, command.Reason);

        var order = await _orderRepository.GetByIdAsync(command.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for cancellation", command.OrderId);
            return;
        }

        // Cancel the order (domain method)
        order.Cancel(command.Reason);

        await _dbContext.SaveChangesWithOutboxAsync(context.CancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled successfully", command.OrderId);
    }
}

/// <summary>
/// Consumer definitions with retry policies
/// </summary>
public class ConfirmOrderCommandConsumerDefinition : ConsumerDefinition<ConfirmOrderCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ConfirmOrderCommandConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
    }
}

public class CancelOrderCommandConsumerDefinition : ConsumerDefinition<CancelOrderCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CancelOrderCommandConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
    }
}
