using EShop.Domain.Aggregates.TaxAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class TaxConfigurationRepository : ITaxConfigurationRepository
{
    private readonly EShopDbContext _context;

    public TaxConfigurationRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<TaxConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TaxConfiguration>()
            .Include(t => t.CategoryRates)
            .Include(t => t.Exemptions)
            .Include(t => t.Rules)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TaxConfiguration?> GetByCountryAsync(string countryCode, string? regionCode = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TaxConfiguration>()
            .Include(t => t.CategoryRates)
            .Include(t => t.Exemptions)
            .Include(t => t.Rules)
            .Where(t => t.CountryCode == countryCode.ToUpperInvariant() && t.IsActive);

        if (!string.IsNullOrEmpty(regionCode))
        {
            query = query.Where(t => t.RegionCode == regionCode.ToUpperInvariant());
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxConfiguration>> GetActiveConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<TaxConfiguration>()
            .Include(t => t.CategoryRates)
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxConfiguration>> GetByTaxTypeAsync(TaxType taxType, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TaxConfiguration>()
            .Where(t => t.TaxType == taxType && t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaxConfiguration?> GetApplicableConfigurationAsync(string countryCode, string? regionCode, DateTime asOf, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TaxConfiguration>()
            .Include(t => t.CategoryRates)
            .Include(t => t.Exemptions)
            .Include(t => t.Rules)
            .Where(t => t.CountryCode == countryCode.ToUpperInvariant() &&
                        t.IsActive &&
                        t.EffectiveFrom <= asOf &&
                        (!t.EffectiveUntil.HasValue || t.EffectiveUntil.Value > asOf));

        if (!string.IsNullOrEmpty(regionCode))
        {
            query = query.Where(t => t.RegionCode == regionCode.ToUpperInvariant());
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(TaxConfiguration taxConfiguration, CancellationToken cancellationToken = default)
    {
        await _context.Set<TaxConfiguration>().AddAsync(taxConfiguration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaxConfiguration taxConfiguration, CancellationToken cancellationToken = default)
    {
        _context.Set<TaxConfiguration>().Update(taxConfiguration);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await GetByIdAsync(id, cancellationToken);
        if (config != null)
        {
            _context.Set<TaxConfiguration>().Remove(config);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
