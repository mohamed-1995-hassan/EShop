using EShop.Domain.Aggregates.AuctionAggregate;

namespace EShop.Domain.Repositories;

public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Auction?> GetByAuctionNumberAsync(string auctionNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetByStatusAsync(AuctionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetEndingSoonAsync(int withinMinutes, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetByBidderIdAsync(Guid bidderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetWonByBidderIdAsync(Guid bidderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetWatchedByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Auction auction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Auction auction, CancellationToken cancellationToken = default);
}
