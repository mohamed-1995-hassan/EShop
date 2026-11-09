using EShop.Domain.Aggregates.BundleAggregate;

namespace EShop.Domain.Repositories;

public interface IBundleRepository
{
    Task<Bundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Bundle?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bundle>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bundle>> GetByTypeAsync(BundleType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bundle>> GetByStatusAsync(BundleStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bundle>> GetBundlesContainingProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bundle>> GetExpiringAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    Task AddAsync(Bundle bundle, CancellationToken cancellationToken = default);
    Task UpdateAsync(Bundle bundle, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
