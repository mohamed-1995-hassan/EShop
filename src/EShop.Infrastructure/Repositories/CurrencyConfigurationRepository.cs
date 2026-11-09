using EShop.Domain.Aggregates.CurrencyAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class CurrencyConfigurationRepository : ICurrencyConfigurationRepository
{
    private readonly EShopDbContext _context;

    public CurrencyConfigurationRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<CurrencyConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CurrencyConfiguration>()
            .Include(c => c.ExchangeRates)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CurrencyConfiguration?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CurrencyConfiguration>()
            .Include(c => c.ExchangeRates.Where(e => e.IsActive))
            .FirstOrDefaultAsync(c => c.Code == code.ToUpperInvariant(), cancellationToken);
    }

    public async Task<CurrencyConfiguration?> GetPrimaryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CurrencyConfiguration>()
            .Include(c => c.ExchangeRates.Where(e => e.IsActive))
            .FirstOrDefaultAsync(c => c.IsPrimary && c.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<CurrencyConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CurrencyConfiguration>()
            .Include(c => c.ExchangeRates.Where(e => e.IsActive))
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CurrencyConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CurrencyConfiguration>()
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CurrencyConfiguration currency, CancellationToken cancellationToken = default)
    {
        await _context.Set<CurrencyConfiguration>().AddAsync(currency, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CurrencyConfiguration currency, CancellationToken cancellationToken = default)
    {
        _context.Set<CurrencyConfiguration>().Update(currency);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
