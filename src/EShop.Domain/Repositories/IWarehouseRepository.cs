using EShop.Domain.Aggregates.InventoryAggregate;
using EShop.Domain.Specifications;

namespace EShop.Domain.Repositories;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Warehouse>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Warehouse>> GetByTypeAsync(WarehouseType type, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetWithLocationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    Task UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    
    // Inventory queries
    Task<int> GetTotalStockAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> GetStockByWarehouseAsync(Guid productId, CancellationToken cancellationToken = default);
}

public interface IStockTransferRepository
{
    Task<StockTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockTransfer?> GetByNumberAsync(string transferNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransfer>> GetByStatusAsync(TransferStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransfer>> GetByWarehouseAsync(Guid warehouseId, bool isSource, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransfer>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
    Task UpdateAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
}
