using EShop.Domain.Aggregates.AuctionAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly EShopDbContext _context;

    public AuctionRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Auction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Auction>()
            .Include(a => a.Bids)
            .Include(a => a.Watchers)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Auction?> GetByAuctionNumberAsync(string auctionNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Auction>()
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.AuctionNumber == auctionNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Auction>()
            .Where(a => a.SellerId == sellerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Active && 
                        a.StartTime <= now && 
                        (a.ActualEndTime ?? a.EndTime) > now)
            .OrderBy(a => a.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> GetByStatusAsync(AuctionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Auction>()
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> GetEndingSoonAsync(int withinMinutes, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var endTime = now.AddMinutes(withinMinutes);
        return await _context.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Active && 
                        (a.ActualEndTime ?? a.EndTime) <= endTime &&
                        (a.ActualEndTime ?? a.EndTime) > now)
            .OrderBy(a => a.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> GetByBidderIdAsync(Guid bidderId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Auction>()
            .Include(a => a.Bids)
            .Where(a => a.Bids.Any(b => b.BidderId == bidderId))
            .OrderByDescending(a => a.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> GetWonByBidderIdAsync(Guid bidderId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Auction>()
            .Where(a => a.CurrentWinnerId == bidderId && 
                        a.Status == AuctionStatus.EndedWithWinner)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> GetWatchedByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Auction>()
            .Include(a => a.Watchers)
            .Where(a => a.Watchers.Any(w => w.CustomerId == customerId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        await _context.Set<Auction>().AddAsync(auction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        _context.Set<Auction>().Update(auction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
