using EShop.Application.EventHandlers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Services;

/// <summary>
/// Fake inventory service for demo purposes.
/// Uses in-memory storage to simulate inventory operations.
/// </summary>
public class FakeInventoryService : IInventoryService
{
    private readonly ILogger<FakeInventoryService> _logger;
    
    // In-memory stock: ProductId -> Available Quantity
    private static readonly ConcurrentDictionary<Guid, int> _stock = new();
    
    // In-memory reservations: OrderId -> List of (ProductId, Quantity)
    private static readonly ConcurrentDictionary<Guid, List<(Guid ProductId, int Quantity)>> _reservations = new();
    
    public FakeInventoryService(ILogger<FakeInventoryService> logger)
    {
        _logger = logger;
        
        // Seed some initial stock
        SeedStock();
    }
    
    private void SeedStock()
    {
        // Add some demo products with stock
        _stock.TryAdd(Guid.Parse("11111111-1111-1111-1111-111111111111"), 100); // MacBook Pro
        _stock.TryAdd(Guid.Parse("22222222-2222-2222-2222-222222222222"), 50);  // iPhone 15
        _stock.TryAdd(Guid.Parse("33333333-3333-3333-3333-333333333333"), 200); // AirPods
    }
    
    public Task ReserveStockAsync(
        Guid productId, 
        int quantity, 
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🔒 INVENTORY: Reserving {Quantity} units of Product {ProductId} for Order {OrderId}",
            quantity, productId, orderId);
        
        // Get or create reservation list for this order
        var orderReservations = _reservations.GetOrAdd(orderId, _ => new List<(Guid, int)>());
        orderReservations.Add((productId, quantity));
        
        // Decrease available stock
        _stock.AddOrUpdate(
            productId,
            key => 1000 - quantity, // Initial if not exists
            (key, current) => current - quantity);
        
        var remaining = _stock.GetValueOrDefault(productId, 0);
        _logger.LogInformation(
            "📊 INVENTORY: Product {ProductId} stock after reservation: {Remaining} units",
            productId, remaining);
        
        return Task.CompletedTask;
    }
    
    public Task ReleaseReservationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🔓 INVENTORY: Releasing reservations for Order {OrderId}",
            orderId);
        
        if (_reservations.TryRemove(orderId, out var reservations))
        {
            foreach (var (productId, quantity) in reservations)
            {
                // Return stock
                _stock.AddOrUpdate(
                    productId,
                    key => quantity,
                    (key, current) => current + quantity);
                
                var remaining = _stock.GetValueOrDefault(productId, 0);
                _logger.LogInformation(
                    "📊 INVENTORY: Product {ProductId} stock after release: {Remaining} units",
                    productId, remaining);
            }
        }
        
        return Task.CompletedTask;
    }
    
    public Task<int> GetAvailableStockAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var stock = _stock.GetValueOrDefault(productId, 0);
        return Task.FromResult(stock);
    }
}
