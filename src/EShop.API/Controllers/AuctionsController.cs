using EShop.Application.Commands.Auctions;
using EShop.Application.Queries.Auctions;
using EShop.Domain.Aggregates.AuctionAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuctionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuctionDto>> Get(Guid id)
    {
        var auction = await _mediator.Send(new GetAuctionQuery(id));
        return auction != null ? Ok(auction) : NotFound();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<AuctionSummaryDto>>> GetActive()
    {
        var auctions = await _mediator.Send(new GetActiveAuctionsQuery());
        return Ok(auctions);
    }

    [HttpGet("ending-soon")]
    public async Task<ActionResult<IReadOnlyList<AuctionSummaryDto>>> GetEndingSoon([FromQuery] int withinMinutes = 60)
    {
        var auctions = await _mediator.Send(new GetAuctionsEndingSoonQuery(withinMinutes));
        return Ok(auctions);
    }

    [HttpGet("seller/{sellerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuctionSummaryDto>>> GetBySeller(Guid sellerId)
    {
        var auctions = await _mediator.Send(new GetSellerAuctionsQuery(sellerId));
        return Ok(auctions);
    }

    [HttpGet("bidder/{bidderId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuctionSummaryDto>>> GetByBidder(Guid bidderId)
    {
        var auctions = await _mediator.Send(new GetBidderAuctionsQuery(bidderId));
        return Ok(auctions);
    }

    [HttpGet("won/{bidderId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuctionSummaryDto>>> GetWon(Guid bidderId)
    {
        var auctions = await _mediator.Send(new GetWonAuctionsQuery(bidderId));
        return Ok(auctions);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateAuctionRequest request)
    {
        var id = await _mediator.Send(new CreateAuctionCommand(
            request.ProductId,
            request.SellerId,
            request.Title,
            request.Description,
            request.StartingPrice,
            request.Currency,
            request.StartTime,
            request.EndTime,
            request.ReservePrice,
            request.BuyNowPrice,
            request.Type,
            request.ImageUrl));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{auctionId:guid}/bid")]
    public async Task<ActionResult<Guid>> PlaceBid(Guid auctionId, [FromBody] PlaceBidRequest request)
    {
        var bidId = await _mediator.Send(new PlaceBidCommand(
            auctionId, request.BidderId, request.BidAmount, request.Currency, request.MaxAutoBid));
        return Ok(bidId);
    }

    [HttpPost("{auctionId:guid}/proxy-bid")]
    public async Task<ActionResult<Guid>> PlaceProxyBid(Guid auctionId, [FromBody] PlaceProxyBidRequest request)
    {
        var bidId = await _mediator.Send(new PlaceProxyBidCommand(
            auctionId, request.BidderId, request.MaxBidAmount, request.Currency));
        return Ok(bidId);
    }

    [HttpPost("{auctionId:guid}/buy-now")]
    public async Task<IActionResult> BuyNow(Guid auctionId, [FromBody] BuyNowRequest request)
    {
        await _mediator.Send(new BuyNowCommand(auctionId, request.BuyerId));
        return NoContent();
    }

    [HttpPost("{auctionId:guid}/end")]
    public async Task<IActionResult> End(Guid auctionId, [FromBody] EndAuctionRequest? request = null)
    {
        await _mediator.Send(new EndAuctionCommand(auctionId, request?.Reason ?? AuctionEndReason.SellerEnded));
        return NoContent();
    }

    [HttpPost("{auctionId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid auctionId, [FromBody] CancelAuctionRequest request)
    {
        await _mediator.Send(new CancelAuctionCommand(auctionId, request.Reason));
        return NoContent();
    }

    [HttpPost("{auctionId:guid}/watch")]
    public async Task<IActionResult> AddWatcher(Guid auctionId, [FromBody] AddWatcherRequest request)
    {
        await _mediator.Send(new AddAuctionWatcherCommand(auctionId, request.CustomerId));
        return NoContent();
    }

    [HttpPost("{auctionId:guid}/paid")]
    public async Task<IActionResult> MarkPaid(Guid auctionId, [FromBody] MarkPaidRequest request)
    {
        await _mediator.Send(new MarkAuctionPaidCommand(auctionId, request.PaymentId));
        return NoContent();
    }
}

// Request DTOs
public record CreateAuctionRequest(
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
    string? ImageUrl = null);

public record PlaceBidRequest(Guid BidderId, decimal BidAmount, string Currency, decimal? MaxAutoBid = null);
public record PlaceProxyBidRequest(Guid BidderId, decimal MaxBidAmount, string Currency);
public record BuyNowRequest(Guid BuyerId);
public record EndAuctionRequest(AuctionEndReason Reason = AuctionEndReason.SellerEnded);
public record CancelAuctionRequest(string Reason);
public record AddWatcherRequest(Guid CustomerId);
public record MarkPaidRequest(Guid PaymentId);
