namespace EShop.Application.Interfaces;

/// <summary>
/// Inventory service interface for stock management.
/// </summary>
public interface IInventoryService
{
    Task ReserveStockAsync(
        Guid productId, 
        int quantity, 
        Guid orderId,
        CancellationToken cancellationToken = default);
    
    Task ReleaseReservationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
    
    Task<int> GetAvailableStockAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
