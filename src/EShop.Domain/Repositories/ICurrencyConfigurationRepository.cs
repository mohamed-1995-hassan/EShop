using EShop.Domain.Aggregates.CurrencyAggregate;

namespace EShop.Domain.Repositories;

public interface ICurrencyConfigurationRepository
{
    Task<CurrencyConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CurrencyConfiguration?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<CurrencyConfiguration?> GetPrimaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrencyConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrencyConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CurrencyConfiguration currency, CancellationToken cancellationToken = default);
    Task UpdateAsync(CurrencyConfiguration currency, CancellationToken cancellationToken = default);
}
