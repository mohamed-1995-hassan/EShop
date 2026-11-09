using EShop.Domain.Aggregates.TaxAggregate;

namespace EShop.Domain.Repositories;

public interface ITaxConfigurationRepository
{
    Task<TaxConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxConfiguration?> GetByCountryAsync(string countryCode, string? regionCode = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxConfiguration>> GetActiveConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxConfiguration>> GetByTaxTypeAsync(TaxType taxType, CancellationToken cancellationToken = default);
    Task<TaxConfiguration?> GetApplicableConfigurationAsync(string countryCode, string? regionCode, DateTime asOf, CancellationToken cancellationToken = default);
    
    Task AddAsync(TaxConfiguration configuration, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaxConfiguration configuration, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
