using EShop.Domain.Aggregates.InventoryAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly EShopDbContext _context;

    public WarehouseRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Warehouse>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Warehouse>()
            .Where(w => w.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Warehouse>> GetByTypeAsync(WarehouseType type, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Warehouse>()
            .Where(w => w.Type == type && w.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Warehouse?> GetWithLocationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Warehouse>()
            .Include(w => w.Locations)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        await _context.Set<Warehouse>().AddAsync(warehouse, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        _context.Set<Warehouse>().Update(warehouse);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetTotalStockAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var warehouses = await _context.Set<Warehouse>()
            .Where(w => w.IsActive)
            .Include(w => w.Locations)
            .ToListAsync(cancellationToken);

        return warehouses.Sum(w => w.GetTotalStockForProduct(productId));
    }

    public async Task<Dictionary<Guid, int>> GetStockByWarehouseAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var warehouses = await _context.Set<Warehouse>()
            .Where(w => w.IsActive)
            .Include(w => w.Locations)
            .ToListAsync(cancellationToken);

        return warehouses.ToDictionary(
            w => w.Id,
            w => w.GetTotalStockForProduct(productId));
    }
}

public class StockTransferRepository : IStockTransferRepository
{
    private readonly EShopDbContext _context;

    public StockTransferRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<StockTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StockTransfer>()
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<StockTransfer?> GetByNumberAsync(string transferNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StockTransfer>()
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.TransferNumber == transferNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransfer>> GetByStatusAsync(TransferStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StockTransfer>()
            .Where(t => t.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransfer>> GetByWarehouseAsync(Guid warehouseId, bool isSource, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StockTransfer>()
            .Where(t => isSource ? t.SourceWarehouseId == warehouseId : t.DestinationWarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransfer>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<StockTransfer>()
            .Where(t => t.Status == TransferStatus.PendingApproval)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        await _context.Set<StockTransfer>().AddAsync(transfer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        _context.Set<StockTransfer>().Update(transfer);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
