using EShop.Domain.Aggregates.SupplierAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private static readonly ConcurrentDictionary<Guid, Supplier> _suppliers = new();

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_suppliers.GetValueOrDefault(id));

    public Task<Supplier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_suppliers.Values.FirstOrDefault(s => s.Code == code.ToUpperInvariant()));

    public Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Supplier>>(_suppliers.Values.Where(s => s.Status == SupplierStatus.Active).ToList());

    public Task<IReadOnlyList<Supplier>> GetByStatusAsync(SupplierStatus status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Supplier>>(_suppliers.Values.Where(s => s.Status == status).ToList());

    public Task<IReadOnlyList<Supplier>> GetByTierAsync(SupplierTier tier, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Supplier>>(_suppliers.Values.Where(s => s.Tier == tier).ToList());

    public Task<IReadOnlyList<Supplier>> GetSuppliersForProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Supplier>>(_suppliers.Values
            .Where(s => s.Status == SupplierStatus.Active && s.Products.Any(p => p.ProductId == productId && p.IsActive))
            .ToList());

    public Task<Supplier?> GetPreferredSupplierForProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.FromResult(_suppliers.Values
            .Where(s => s.Status == SupplierStatus.Active && s.Products.Any(p => p.ProductId == productId && p.IsPreferred))
            .OrderByDescending(s => s.PerformanceScore)
            .FirstOrDefault());

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _suppliers[supplier.Id] = supplier;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _suppliers[supplier.Id] = supplier;
        return Task.CompletedTask;
    }
}

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private static readonly ConcurrentDictionary<Guid, PurchaseOrder> _orders = new();

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_orders.GetValueOrDefault(id));

    public Task<PurchaseOrder?> GetByPoNumberAsync(string poNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_orders.Values.FirstOrDefault(o => o.PoNumber == poNumber));

    public Task<IReadOnlyList<PurchaseOrder>> GetBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PurchaseOrder>>(_orders.Values.Where(o => o.SupplierId == supplierId).OrderByDescending(o => o.CreatedAt).ToList());

    public Task<IReadOnlyList<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PurchaseOrder>>(_orders.Values.Where(o => o.Status == status).ToList());

    public Task<IReadOnlyList<PurchaseOrder>> GetPendingApprovalAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PurchaseOrder>>(_orders.Values.Where(o => o.Status == PurchaseOrderStatus.PendingApproval).OrderBy(o => o.CreatedAt).ToList());

    public Task<IReadOnlyList<PurchaseOrder>> GetAwaitingDeliveryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PurchaseOrder>>(_orders.Values
            .Where(o => o.Status == PurchaseOrderStatus.Sent || o.Status == PurchaseOrderStatus.Acknowledged || o.Status == PurchaseOrderStatus.PartiallyReceived)
            .OrderBy(o => o.ExpectedDeliveryDate)
            .ToList());

    public Task AddAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
