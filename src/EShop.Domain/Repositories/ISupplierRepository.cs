using EShop.Domain.Aggregates.SupplierAggregate;

namespace EShop.Domain.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> GetByStatusAsync(SupplierStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> GetByTierAsync(SupplierTier tier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> GetSuppliersForProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Supplier?> GetPreferredSupplierForProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
}

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetByPoNumberAsync(string poNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetAwaitingDeliveryAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
}
