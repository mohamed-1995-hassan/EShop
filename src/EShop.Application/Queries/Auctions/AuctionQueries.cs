using EShop.Domain.Aggregates.AuctionAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Auctions;

// DTOs
public record AuctionDto(
    Guid Id,
    string AuctionNumber,
    Guid ProductId,
    Guid SellerId,
    string Title,
    string Description,
    string? ImageUrl,
    AuctionStatus Status,
    AuctionType Type,
    decimal StartingPrice,
    decimal CurrentBid,
    decimal? ReservePrice,
    decimal? BuyNowPrice,
    string Currency,
    int BidCount,
    Guid? CurrentWinnerId,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? ActualEndTime,
    bool IsActive,
    bool ReserveMet,
    bool HasBuyNow,
    TimeSpan TimeRemaining,
    IReadOnlyList<BidDto> Bids);

public record BidDto(
    Guid Id,
    Guid BidderId,
    decimal Amount,
    string Currency,
    BidType Type,
    bool IsOutbid,
    bool IsWinning,
    DateTime PlacedAt);

public record AuctionSummaryDto(
    Guid Id,
    string AuctionNumber,
    string Title,
    decimal CurrentBid,
    string Currency,
    int BidCount,
    AuctionStatus Status,
    DateTime EndTime,
    bool IsActive);

// Get Auction by ID
public record GetAuctionQuery(Guid Id) : IRequest<AuctionDto?>;

public class GetAuctionQueryHandler : IRequestHandler<GetAuctionQuery, AuctionDto?>
{
    private readonly IAuctionRepository _repository;

    public GetAuctionQueryHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<AuctionDto?> Handle(GetAuctionQuery request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return auction == null ? null : MapToDto(auction);
    }

    private static AuctionDto MapToDto(Auction a) => new(
        a.Id,
        a.AuctionNumber,
        a.ProductId,
        a.SellerId,
        a.Title,
        a.Description,
        a.ImageUrl,
        a.Status,
        a.Type,
        a.StartingPrice.Amount,
        a.CurrentBid.Amount,
        a.ReservePrice?.Amount,
        a.BuyNowPrice?.Amount,
        a.CurrentBid.Currency,
        a.BidCount,
        a.CurrentWinnerId,
        a.StartTime,
        a.EndTime,
        a.ActualEndTime,
        a.IsActive,
        a.ReserveMet,
        a.HasBuyNow,
        a.TimeRemaining,
        a.Bids.Select(b => new BidDto(
            b.Id, b.BidderId, b.BidAmount.Amount, b.BidAmount.Currency,
            b.Type, b.IsOutbid, b.IsWinning, b.PlacedAt)).ToList());
}

// Get Active Auctions
public record GetActiveAuctionsQuery : IRequest<IReadOnlyList<AuctionSummaryDto>>;

public class GetActiveAuctionsQueryHandler : IRequestHandler<GetActiveAuctionsQuery, IReadOnlyList<AuctionSummaryDto>>
{
    private readonly IAuctionRepository _repository;

    public GetActiveAuctionsQueryHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AuctionSummaryDto>> Handle(GetActiveAuctionsQuery request, CancellationToken cancellationToken)
    {
        var auctions = await _repository.GetActiveAsync(cancellationToken);
        return auctions.Select(a => new AuctionSummaryDto(
            a.Id, a.AuctionNumber, a.Title, a.CurrentBid.Amount, a.CurrentBid.Currency,
            a.BidCount, a.Status, a.EffectiveEndTime, a.IsActive)).ToList();
    }
}

// Get Auctions Ending Soon
public record GetAuctionsEndingSoonQuery(int WithinMinutes = 60) : IRequest<IReadOnlyList<AuctionSummaryDto>>;

public class GetAuctionsEndingSoonQueryHandler : IRequestHandler<GetAuctionsEndingSoonQuery, IReadOnlyList<AuctionSummaryDto>>
{
    private readonly IAuctionRepository _repository;

    public GetAuctionsEndingSoonQueryHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AuctionSummaryDto>> Handle(GetAuctionsEndingSoonQuery request, CancellationToken cancellationToken)
    {
        var auctions = await _repository.GetEndingSoonAsync(request.WithinMinutes, cancellationToken);
        return auctions.Select(a => new AuctionSummaryDto(
            a.Id, a.AuctionNumber, a.Title, a.CurrentBid.Amount, a.CurrentBid.Currency,
            a.BidCount, a.Status, a.EffectiveEndTime, a.IsActive)).ToList();
    }
}

// Get Auctions by Seller
public record GetSellerAuctionsQuery(Guid SellerId) : IRequest<IReadOnlyList<AuctionSummaryDto>>;

public class GetSellerAuctionsQueryHandler : IRequestHandler<GetSellerAuctionsQuery, IReadOnlyList<AuctionSummaryDto>>
{
    private readonly IAuctionRepository _repository;

    public GetSellerAuctionsQueryHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AuctionSummaryDto>> Handle(GetSellerAuctionsQuery request, CancellationToken cancellationToken)
    {
        var auctions = await _repository.GetBySellerIdAsync(request.SellerId, cancellationToken);
        return auctions.Select(a => new AuctionSummaryDto(
            a.Id, a.AuctionNumber, a.Title, a.CurrentBid.Amount, a.CurrentBid.Currency,
            a.BidCount, a.Status, a.EffectiveEndTime, a.IsActive)).ToList();
    }
}

// Get Auctions by Bidder
public record GetBidderAuctionsQuery(Guid BidderId) : IRequest<IReadOnlyList<AuctionSummaryDto>>;

public class GetBidderAuctionsQueryHandler : IRequestHandler<GetBidderAuctionsQuery, IReadOnlyList<AuctionSummaryDto>>
{
    private readonly IAuctionRepository _repository;

    public GetBidderAuctionsQueryHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AuctionSummaryDto>> Handle(GetBidderAuctionsQuery request, CancellationToken cancellationToken)
    {
        var auctions = await _repository.GetByBidderIdAsync(request.BidderId, cancellationToken);
        return auctions.Select(a => new AuctionSummaryDto(
            a.Id, a.AuctionNumber, a.Title, a.CurrentBid.Amount, a.CurrentBid.Currency,
            a.BidCount, a.Status, a.EffectiveEndTime, a.IsActive)).ToList();
    }
}

// Get Won Auctions
public record GetWonAuctionsQuery(Guid BidderId) : IRequest<IReadOnlyList<AuctionSummaryDto>>;

public class GetWonAuctionsQueryHandler : IRequestHandler<GetWonAuctionsQuery, IReadOnlyList<AuctionSummaryDto>>
{
    private readonly IAuctionRepository _repository;

    public GetWonAuctionsQueryHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AuctionSummaryDto>> Handle(GetWonAuctionsQuery request, CancellationToken cancellationToken)
    {
        var auctions = await _repository.GetWonByBidderIdAsync(request.BidderId, cancellationToken);
        return auctions.Select(a => new AuctionSummaryDto(
            a.Id, a.AuctionNumber, a.Title, a.CurrentBid.Amount, a.CurrentBid.Currency,
            a.BidCount, a.Status, a.EffectiveEndTime, a.IsActive)).ToList();
    }
}
