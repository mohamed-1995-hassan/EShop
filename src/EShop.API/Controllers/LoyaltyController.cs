using EShop.Application.Commands.Loyalty;
using EShop.Application.Queries.Loyalty;
using EShop.Domain.Aggregates.LoyaltyAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoyaltyController : ControllerBase
{
    private readonly IMediator _mediator;

    public LoyaltyController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoyaltyAccountDto>> Get(Guid id)
    {
        var account = await _mediator.Send(new GetLoyaltyAccountQuery(id));
        return account != null ? Ok(account) : NotFound();
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<LoyaltyAccountDto>> GetByCustomer(Guid customerId)
    {
        var account = await _mediator.Send(new GetLoyaltyAccountByCustomerQuery(customerId));
        return account != null ? Ok(account) : NotFound();
    }

    [HttpGet("tier/{tier}")]
    public async Task<ActionResult<IReadOnlyList<LoyaltySummaryDto>>> GetByTier(LoyaltyTier tier)
    {
        var accounts = await _mediator.Send(new GetAccountsByTierQuery(tier));
        return Ok(accounts);
    }

    [HttpGet("top/{count:int}")]
    public async Task<ActionResult<IReadOnlyList<LoyaltySummaryDto>>> GetTopMembers(int count = 10)
    {
        var accounts = await _mediator.Send(new GetTopMembersQuery(count));
        return Ok(accounts);
    }

    [HttpGet("expiring/{days:int}")]
    public async Task<ActionResult<IReadOnlyList<LoyaltySummaryDto>>> GetWithExpiringPoints(int days = 30)
    {
        var accounts = await _mediator.Send(new GetAccountsWithExpiringPointsQuery(days));
        return Ok(accounts);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<LoyaltyProgramStatsDto>> GetStats()
    {
        var stats = await _mediator.Send(new GetLoyaltyProgramStatsQuery());
        return Ok(stats);
    }

    [HttpPost("enroll")]
    public async Task<ActionResult<Guid>> Enroll([FromBody] EnrollRequest request)
    {
        var id = await _mediator.Send(new EnrollCustomerCommand(request.CustomerId, request.WelcomeBonus));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{accountId:guid}/earn")]
    public async Task<IActionResult> EarnPoints(Guid accountId, [FromBody] EarnPointsRequest request)
    {
        await _mediator.Send(new EarnPointsCommand(
            accountId, request.OrderId, request.OrderAmount, request.Currency, request.PointsPerCurrency));
        return NoContent();
    }

    [HttpPost("{accountId:guid}/bonus")]
    public async Task<IActionResult> EarnBonusPoints(Guid accountId, [FromBody] EarnBonusRequest request)
    {
        await _mediator.Send(new EarnBonusPointsCommand(accountId, request.Points, request.Reason, request.Type));
        return NoContent();
    }

    [HttpPost("{accountId:guid}/redeem/discount")]
    public async Task<ActionResult<PointRedemption>> RedeemForDiscount(Guid accountId, [FromBody] RedeemDiscountRequest request)
    {
        var redemption = await _mediator.Send(new RedeemPointsForDiscountCommand(
            accountId, request.Points, request.DiscountAmount, request.Currency, request.OrderId));
        return Ok(redemption);
    }

    [HttpPost("{accountId:guid}/redeem/reward")]
    public async Task<ActionResult<PointRedemption>> RedeemForReward(Guid accountId, [FromBody] RedeemRewardRequest request)
    {
        var redemption = await _mediator.Send(new RedeemPointsForRewardCommand(
            accountId, request.RewardId, request.RewardName, request.RequiredPoints));
        return Ok(redemption);
    }

    [HttpPost("{accountId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid accountId, [FromBody] SuspendRequest request)
    {
        await _mediator.Send(new SuspendLoyaltyAccountCommand(accountId, request.Reason));
        return NoContent();
    }

    [HttpPost("{accountId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid accountId)
    {
        await _mediator.Send(new ReactivateLoyaltyAccountCommand(accountId));
        return NoContent();
    }
}

// Request DTOs
public record EnrollRequest(Guid CustomerId, int WelcomeBonus = 100);
public record EarnPointsRequest(Guid OrderId, decimal OrderAmount, string Currency, decimal PointsPerCurrency = 1);
public record EarnBonusRequest(int Points, string Reason, PointTransactionType Type = PointTransactionType.Bonus);
public record RedeemDiscountRequest(int Points, decimal DiscountAmount, string Currency, Guid OrderId);
public record RedeemRewardRequest(Guid RewardId, string RewardName, int RequiredPoints);
public record SuspendRequest(string Reason);
