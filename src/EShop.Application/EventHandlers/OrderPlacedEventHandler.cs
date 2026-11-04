using EShop.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EShop.Application.EventHandlers;

/// <summary>
/// Handles OrderPlacedEvent - sends confirmation email.
/// 
/// KEY POINTS:
/// - Handler is in Application layer, not Domain
/// - Domain doesn't know about email, only that event occurred
/// - Multiple handlers can react to same event
/// </summary>
public class SendOrderConfirmationHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly ILogger<SendOrderConfirmationHandler> _logger;
    private readonly IEmailService _emailService;
    
    public SendOrderConfirmationHandler(
        ILogger<SendOrderConfirmationHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }
    
    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "📧 Sending order confirmation email for Order {OrderId} to Customer {CustomerId}",
            notification.OrderId,
            notification.CustomerId);
        
        // Build email content
        var itemsList = string.Join("\n", notification.Items.Select(i => 
            $"  - {i.ProductName} x{i.Quantity} @ {i.UnitPrice:C}"));
        
        await _emailService.SendEmailAsync(
            to: $"customer-{notification.CustomerId}@email.com",
            subject: $"Order Confirmation - {notification.OrderId}",
            body: $"""
                Thank you for your order!
                
                Order ID: {notification.OrderId}
                Total: {notification.TotalAmount:C} {notification.Currency}
                
                Items:
                {itemsList}
                
                We will notify you when your order ships.
                """,
            cancellationToken);
        
        _logger.LogInformation("✅ Order confirmation email sent for Order {OrderId}", notification.OrderId);
    }
}

/// <summary>
/// Another handler for OrderPlacedEvent - reserves inventory.
/// Demonstrates: Multiple handlers for same event (loose coupling)
/// </summary>
public class ReserveInventoryHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly ILogger<ReserveInventoryHandler> _logger;
    private readonly IInventoryService _inventoryService;
    
    public ReserveInventoryHandler(
        ILogger<ReserveInventoryHandler> logger,
        IInventoryService inventoryService)
    {
        _logger = logger;
        _inventoryService = inventoryService;
    }
    
    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "📦 Reserving inventory for Order {OrderId}",
            notification.OrderId);
        
        foreach (var item in notification.Items)
        {
            await _inventoryService.ReserveStockAsync(
                item.ProductId,
                item.Quantity,
                notification.OrderId,
                cancellationToken);
            
            _logger.LogInformation(
                "  Reserved {Quantity} units of Product {ProductId}",
                item.Quantity,
                item.ProductId);
        }
        
        _logger.LogInformation("✅ Inventory reserved for Order {OrderId}", notification.OrderId);
    }
}

/// <summary>
/// Handler for analytics/reporting
/// </summary>
public class UpdateOrderAnalyticsHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly ILogger<UpdateOrderAnalyticsHandler> _logger;
    
    public UpdateOrderAnalyticsHandler(ILogger<UpdateOrderAnalyticsHandler> logger)
    {
        _logger = logger;
    }
    
    public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "📊 Recording analytics: Order {OrderId}, Amount: {Amount} {Currency}, Items: {ItemCount}",
            notification.OrderId,
            notification.TotalAmount,
            notification.Currency,
            notification.Items.Count);
        
        // In real app: send to analytics service, update reports, etc.
        return Task.CompletedTask;
    }
}
