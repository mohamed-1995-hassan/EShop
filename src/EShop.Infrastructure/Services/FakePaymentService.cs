using EShop.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Services;

/// <summary>
/// Fake payment service for demo purposes.
/// In real app: would integrate with Stripe, PayPal, etc.
/// </summary>
public class FakePaymentService : IPaymentService
{
    private readonly ILogger<FakePaymentService> _logger;
    
    public FakePaymentService(ILogger<FakePaymentService> logger)
    {
        _logger = logger;
    }
    
    public Task<bool> ProcessPaymentAsync(
        Guid orderId,
        Guid customerId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            
            ═══════════════════════════════════════════════════
            💳 PAYMENT PROCESSED (Simulated)
            ───────────────────────────────────────────────────
            Order ID:    {OrderId}
            Customer ID: {CustomerId}
            Amount:      {Amount:C} {Currency}
            Status:      SUCCESS
            ═══════════════════════════════════════════════════
            
            """,
            orderId, customerId, amount, currency);
        
        return Task.FromResult(true);
    }
    
    public Task RefundAsync(
        Guid orderId,
        Guid customerId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            
            ═══════════════════════════════════════════════════
            💰 REFUND PROCESSED (Simulated)
            ───────────────────────────────────────────────────
            Order ID:    {OrderId}
            Customer ID: {CustomerId}
            Amount:      {Amount:C}
            Reason:      {Reason}
            Status:      SUCCESS
            ═══════════════════════════════════════════════════
            
            """,
            orderId, customerId, amount, reason);
        
        return Task.CompletedTask;
    }
}
