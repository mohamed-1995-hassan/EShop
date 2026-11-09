using EShop.Domain.Aggregates.GiftCardAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class GiftCardRepository : IGiftCardRepository
{
    private readonly EShopDbContext _context;

    public GiftCardRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<GiftCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .Include(g => g.Transactions)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<GiftCard?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .Include(g => g.Transactions)
            .FirstOrDefaultAsync(g => g.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<GiftCard>> GetByPurchaserIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .Where(g => g.PurchasedByCustomerId == customerId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GiftCard>> GetByRecipientIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .Where(g => g.RecipientCustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GiftCard>> GetByStatusAsync(GiftCardStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .Where(g => g.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GiftCard>> GetExpiringSoonAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Set<GiftCard>()
            .Where(g => g.Status == GiftCardStatus.Active &&
                        g.ExpiresAt.HasValue &&
                        g.ExpiresAt.Value <= expiryDate &&
                        g.CurrentBalance.Amount > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GiftCard>> GetPendingDeliveryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .Where(g => g.Type == GiftCardType.Digital && 
                        !g.IsDelivered &&
                        g.ScheduledDeliveryDate.HasValue &&
                        g.ScheduledDeliveryDate.Value <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .AnyAsync(g => g.Code == code, cancellationToken);
    }

    public async Task<decimal> GetTotalUnredeemedBalanceAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .Where(g => g.Status == GiftCardStatus.Active)
            .SumAsync(g => g.CurrentBalance.Amount, cancellationToken);
    }

    public async Task<decimal> GetTotalRedeemedAmountAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GiftCard>()
            .SelectMany(g => g.Transactions)
            .Where(t => t.Type == GiftCardTransactionType.Redemption &&
                        t.CreatedAt >= startDate &&
                        t.CreatedAt <= endDate)
            .SumAsync(t => Math.Abs(t.Amount.Amount), cancellationToken);
    }

    public async Task AddAsync(GiftCard giftCard, CancellationToken cancellationToken = default)
    {
        await _context.Set<GiftCard>().AddAsync(giftCard, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(GiftCard giftCard, CancellationToken cancellationToken = default)
    {
        _context.Set<GiftCard>().Update(giftCard);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
