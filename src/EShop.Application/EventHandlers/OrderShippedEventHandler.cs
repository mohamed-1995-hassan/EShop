using EShop.Application.Interfaces;
using EShop.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EShop.Application.EventHandlers;

/// <summary>
/// Handles OrderShippedEvent - notifies customer about shipping.
/// </summary>
public class SendShippingNotificationHandler : INotificationHandler<OrderShippedEvent>
{
    private readonly ILogger<SendShippingNotificationHandler> _logger;
    private readonly IEmailService _emailService;
    
    public SendShippingNotificationHandler(
        ILogger<SendShippingNotificationHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }
    
    public async Task Handle(OrderShippedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "🚚 Sending shipping notification for Order {OrderId}",
            notification.OrderId);
        
        var trackingInfo = !string.IsNullOrEmpty(notification.TrackingNumber)
            ? $"Tracking Number: {notification.TrackingNumber}\nCarrier: {notification.Carrier}"
            : "Tracking information will be available soon.";
        
        await _emailService.SendEmailAsync(
            to: "customer@email.com", // Would lookup from order
            subject: $"Your Order {notification.OrderId} Has Shipped!",
            body: $"""
                Great news! Your order is on its way!
                
                Order ID: {notification.OrderId}
                {trackingInfo}
                
                Estimated Delivery: {notification.EstimatedDelivery:D}
                
                Thank you for shopping with us!
                """,
            cancellationToken);
        
        _logger.LogInformation("✅ Shipping notification sent for Order {OrderId}", notification.OrderId);
    }
}
