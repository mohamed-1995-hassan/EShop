using EShop.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EShop.Application.EventHandlers;

/// <summary>
/// Handles OrderCancelledEvent - releases inventory.
/// </summary>
public class ReleaseInventoryHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly ILogger<ReleaseInventoryHandler> _logger;
    private readonly IInventoryService _inventoryService;
    
    public ReleaseInventoryHandler(
        ILogger<ReleaseInventoryHandler> logger,
        IInventoryService inventoryService)
    {
        _logger = logger;
        _inventoryService = inventoryService;
    }
    
    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "📦 Releasing reserved inventory for cancelled Order {OrderId}",
            notification.OrderId);
        
        await _inventoryService.ReleaseReservationAsync(
            notification.OrderId,
            cancellationToken);
        
        _logger.LogInformation("✅ Inventory released for Order {OrderId}", notification.OrderId);
    }
}

/// <summary>
/// Handles OrderCancelledEvent - processes refund if applicable.
/// </summary>
public class ProcessRefundHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly ILogger<ProcessRefundHandler> _logger;
    private readonly IPaymentService _paymentService;
    
    public ProcessRefundHandler(
        ILogger<ProcessRefundHandler> logger,
        IPaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }
    
    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        if (notification.RefundAmount <= 0)
        {
            _logger.LogInformation(
                "💰 No refund needed for Order {OrderId} (not yet paid)",
                notification.OrderId);
            return;
        }
        
        _logger.LogInformation(
            "💰 Processing refund of {Amount:C} for Order {OrderId}",
            notification.RefundAmount,
            notification.OrderId);
        
        await _paymentService.RefundAsync(
            notification.OrderId,
            notification.CustomerId,
            notification.RefundAmount,
            notification.Reason,
            cancellationToken);
        
        _logger.LogInformation("✅ Refund processed for Order {OrderId}", notification.OrderId);
    }
}

/// <summary>
/// Handles OrderCancelledEvent - sends cancellation email.
/// </summary>
public class SendCancellationEmailHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly ILogger<SendCancellationEmailHandler> _logger;
    private readonly IEmailService _emailService;
    
    public SendCancellationEmailHandler(
        ILogger<SendCancellationEmailHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }
    
    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "📧 Sending cancellation email for Order {OrderId}",
            notification.OrderId);
        
        var refundMessage = notification.RefundAmount > 0
            ? $"A refund of {notification.RefundAmount:C} will be processed within 5-7 business days."
            : "";
        
        await _emailService.SendEmailAsync(
            to: $"customer-{notification.CustomerId}@email.com",
            subject: $"Order {notification.OrderId} Cancelled",
            body: $"""
                Your order has been cancelled.
                
                Order ID: {notification.OrderId}
                Reason: {notification.Reason}
                
                {refundMessage}
                
                If you have any questions, please contact support.
                """,
            cancellationToken);
        
        _logger.LogInformation("✅ Cancellation email sent for Order {OrderId}", notification.OrderId);
    }
}
