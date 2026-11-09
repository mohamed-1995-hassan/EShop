using EShop.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EShop.Application.EventHandlers;

/// <summary>
/// Handler for ProductLowStockEvent
/// 
/// RESPONSIBILITIES:
/// - Send notification to inventory team
/// - Trigger auto-reorder if configured
/// - Update dashboard/analytics
/// </summary>
public class ProductLowStockEventHandler : INotificationHandler<ProductLowStockEvent>
{
    private readonly ILogger<ProductLowStockEventHandler> _logger;

    public ProductLowStockEventHandler(ILogger<ProductLowStockEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProductLowStockEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "LOW STOCK ALERT: Product {Sku} has only {CurrentStock} units remaining (threshold: {Threshold})",
            notification.Sku,
            notification.CurrentStock,
            notification.Threshold);

        // TODO: Send email/SMS to inventory team
        // TODO: Check if auto-reorder is enabled
        // TODO: Update real-time dashboard

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for ProductOutOfStockEvent
/// </summary>
public class ProductOutOfStockEventHandler : INotificationHandler<ProductOutOfStockEvent>
{
    private readonly ILogger<ProductOutOfStockEventHandler> _logger;

    public ProductOutOfStockEventHandler(ILogger<ProductOutOfStockEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProductOutOfStockEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogError(
            "OUT OF STOCK: Product {Sku} is now out of stock!",
            notification.Sku);

        // TODO: Hide product from website
        // TODO: Urgent notification to purchasing
        // TODO: Notify customers with items in cart

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for ProductPriceChangedEvent
/// </summary>
public class ProductPriceChangedEventHandler : INotificationHandler<ProductPriceChangedEvent>
{
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public ProductPriceChangedEventHandler(ILogger<ProductPriceChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProductPriceChangedEvent notification, CancellationToken cancellationToken)
    {
        var changePercent = notification.OldPrice > 0
            ? ((notification.NewPrice - notification.OldPrice) / notification.OldPrice) * 100
            : 0;

        _logger.LogInformation(
            "Price changed for {Sku}: {OldPrice} -> {NewPrice} {Currency} ({Change:+0.00;-0.00}%)",
            notification.Sku,
            notification.OldPrice,
            notification.NewPrice,
            notification.Currency,
            changePercent);

        // TODO: Update price history
        // TODO: If price dropped, notify customers who have it in wishlist
        // TODO: Update search index

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for ProductStockReservedEvent - Integration with Order aggregate
/// </summary>
public class ProductStockReservedEventHandler : INotificationHandler<ProductStockReservedEvent>
{
    private readonly ILogger<ProductStockReservedEventHandler> _logger;

    public ProductStockReservedEventHandler(ILogger<ProductStockReservedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProductStockReservedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stock reserved: {Quantity} units of {Sku} for Order {OrderId}. Remaining: {Remaining}",
            notification.QuantityReserved,
            notification.Sku,
            notification.OrderId,
            notification.RemainingStock);

        return Task.CompletedTask;
    }
}
