using EShop.Domain.Aggregates.DigitalProductAggregate;

namespace EShop.Domain.Repositories;

public interface IDigitalProductRepository
{
    Task<DigitalProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DigitalProduct?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DigitalProduct>> GetByTypeAsync(DigitalProductType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DigitalProduct>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<LicenseKey?> GetLicenseByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LicenseKey>> GetLicensesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(DigitalProduct product, CancellationToken cancellationToken = default);
    Task UpdateAsync(DigitalProduct product, CancellationToken cancellationToken = default);
}
