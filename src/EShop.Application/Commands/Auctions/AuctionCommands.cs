using EShop.Domain.Aggregates.AuctionAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Auctions;

// Create Auction
public record CreateAuctionCommand(
    Guid ProductId,
    Guid SellerId,
    string Title,
    string Description,
    decimal StartingPrice,
    string Currency,
    DateTime StartTime,
    DateTime EndTime,
    decimal? ReservePrice = null,
    decimal? BuyNowPrice = null,
    AuctionType Type = AuctionType.Standard,
    string? ImageUrl = null) : IRequest<Guid>;

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Guid>
{
    private readonly IAuctionRepository _repository;

    public CreateAuctionCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = Auction.Create(
            request.ProductId,
            request.SellerId,
            request.Title,
            request.Description,
            new Money(request.StartingPrice, request.Currency),
            request.StartTime,
            request.EndTime,
            request.ReservePrice.HasValue ? new Money(request.ReservePrice.Value, request.Currency) : null,
            request.BuyNowPrice.HasValue ? new Money(request.BuyNowPrice.Value, request.Currency) : null,
            request.Type,
            request.ImageUrl);

        await _repository.AddAsync(auction, cancellationToken);
        return auction.Id;
    }
}

// Place Bid
public record PlaceBidCommand(
    Guid AuctionId,
    Guid BidderId,
    decimal BidAmount,
    string Currency,
    decimal? MaxAutoBid = null) : IRequest<Guid>;

public class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, Guid>
{
    private readonly IAuctionRepository _repository;

    public PlaceBidCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<Guid> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Auction not found");

        var bid = auction.PlaceBid(
            request.BidderId,
            new Money(request.BidAmount, request.Currency),
            request.MaxAutoBid.HasValue ? new Money(request.MaxAutoBid.Value, request.Currency) : null);

        await _repository.UpdateAsync(auction, cancellationToken);
        return bid.Id;
    }
}

// Place Proxy Bid
public record PlaceProxyBidCommand(
    Guid AuctionId,
    Guid BidderId,
    decimal MaxBidAmount,
    string Currency) : IRequest<Guid>;

public class PlaceProxyBidCommandHandler : IRequestHandler<PlaceProxyBidCommand, Guid>
{
    private readonly IAuctionRepository _repository;

    public PlaceProxyBidCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task<Guid> Handle(PlaceProxyBidCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Auction not found");

        var bid = auction.PlaceProxyBid(request.BidderId, new Money(request.MaxBidAmount, request.Currency));
        await _repository.UpdateAsync(auction, cancellationToken);
        return bid.Id;
    }
}

// Buy Now
public record BuyNowCommand(Guid AuctionId, Guid BuyerId) : IRequest;

public class BuyNowCommandHandler : IRequestHandler<BuyNowCommand>
{
    private readonly IAuctionRepository _repository;

    public BuyNowCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task Handle(BuyNowCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Auction not found");

        auction.BuyNow(request.BuyerId);
        await _repository.UpdateAsync(auction, cancellationToken);
    }
}

// End Auction
public record EndAuctionCommand(Guid AuctionId, AuctionEndReason Reason = AuctionEndReason.TimeExpired) : IRequest;

public class EndAuctionCommandHandler : IRequestHandler<EndAuctionCommand>
{
    private readonly IAuctionRepository _repository;

    public EndAuctionCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task Handle(EndAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Auction not found");

        auction.EndAuction(request.Reason);
        await _repository.UpdateAsync(auction, cancellationToken);
    }
}

// Cancel Auction
public record CancelAuctionCommand(Guid AuctionId, string Reason) : IRequest;

public class CancelAuctionCommandHandler : IRequestHandler<CancelAuctionCommand>
{
    private readonly IAuctionRepository _repository;

    public CancelAuctionCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task Handle(CancelAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Auction not found");

        auction.Cancel(request.Reason);
        await _repository.UpdateAsync(auction, cancellationToken);
    }
}

// Add Watcher
public record AddAuctionWatcherCommand(Guid AuctionId, Guid CustomerId) : IRequest;

public class AddAuctionWatcherCommandHandler : IRequestHandler<AddAuctionWatcherCommand>
{
    private readonly IAuctionRepository _repository;

    public AddAuctionWatcherCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task Handle(AddAuctionWatcherCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Auction not found");

        auction.AddWatcher(request.CustomerId);
        await _repository.UpdateAsync(auction, cancellationToken);
    }
}

// Mark as Paid
public record MarkAuctionPaidCommand(Guid AuctionId, Guid PaymentId) : IRequest;

public class MarkAuctionPaidCommandHandler : IRequestHandler<MarkAuctionPaidCommand>
{
    private readonly IAuctionRepository _repository;

    public MarkAuctionPaidCommandHandler(IAuctionRepository repository) => _repository = repository;

    public async Task Handle(MarkAuctionPaidCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Auction not found");

        auction.MarkAsPaid(request.PaymentId);
        await _repository.UpdateAsync(auction, cancellationToken);
    }
}
