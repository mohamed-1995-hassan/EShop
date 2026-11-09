using EShop.Application.Commands.Affiliates;
using EShop.Application.Queries.Affiliates;
using EShop.Domain.Aggregates.AffiliateAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AffiliatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AffiliatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AffiliateDto>> Get(Guid id)
    {
        var affiliate = await _mediator.Send(new GetAffiliateQuery(id));
        return affiliate != null ? Ok(affiliate) : NotFound();
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<AffiliateDto>> GetByCode(string code)
    {
        var affiliate = await _mediator.Send(new GetAffiliateByCodeQuery(code));
        return affiliate != null ? Ok(affiliate) : NotFound();
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<AffiliateDto>> GetByUser(Guid userId)
    {
        var affiliate = await _mediator.Send(new GetAffiliateByUserQuery(userId));
        return affiliate != null ? Ok(affiliate) : NotFound();
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<AffiliateSummaryDto>>> GetPending()
    {
        var affiliates = await _mediator.Send(new GetPendingAffiliatesQuery());
        return Ok(affiliates);
    }

    [HttpGet("top-performers")]
    public async Task<ActionResult<IReadOnlyList<AffiliateSummaryDto>>> GetTopPerformers([FromQuery] int count = 10)
    {
        var affiliates = await _mediator.Send(new GetTopPerformersQuery(count));
        return Ok(affiliates);
    }

    [HttpGet("pending-payouts")]
    public async Task<ActionResult<IReadOnlyList<AffiliateSummaryDto>>> GetPendingPayouts()
    {
        var affiliates = await _mediator.Send(new GetPendingPayoutsQuery());
        return Ok(affiliates);
    }

    [HttpPost("apply")]
    public async Task<ActionResult<Guid>> Apply([FromBody] ApplyRequest request)
    {
        var id = await _mediator.Send(new ApplyForAffiliateCommand(request.UserId, request.Name, request.Email, request.Website));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _mediator.Send(new ApproveAffiliateCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AffiliateRejectRequest request)
    {
        await _mediator.Send(new RejectAffiliateCommand(id, request.Reason));
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] AffiliateSuspendRequest request)
    {
        await _mediator.Send(new SuspendAffiliateCommand(id, request.Reason));
        return NoContent();
    }

    [HttpPost("{id:guid}/referrals")]
    public async Task<ActionResult<Guid>> TrackReferral(Guid id, [FromBody] TrackReferralRequest request)
    {
        var referralId = await _mediator.Send(new TrackReferralCommand(id, request.IpAddress, request.UserAgent, request.LandingPage));
        return Ok(referralId);
    }

    [HttpPost("{id:guid}/referrals/{referralId:guid}/convert")]
    public async Task<IActionResult> ConvertReferral(Guid id, Guid referralId, [FromBody] ConvertReferralRequest request)
    {
        await _mediator.Send(new ConvertReferralCommand(id, referralId, request.OrderId, request.CustomerId));
        return NoContent();
    }

    [HttpPost("{id:guid}/commissions")]
    public async Task<ActionResult<Guid>> EarnCommission(Guid id, [FromBody] EarnCommissionRequest request)
    {
        var commissionId = await _mediator.Send(new EarnCommissionCommand(id, request.OrderId, request.OrderAmount, request.Currency, request.Description));
        return Ok(commissionId);
    }

    [HttpPost("{id:guid}/payouts")]
    public async Task<ActionResult<Guid>> RequestPayout(Guid id, [FromBody] PayoutRequest request)
    {
        var payoutId = await _mediator.Send(new RequestPayoutCommand(id, request.Method));
        return Ok(payoutId);
    }

    [HttpPost("{id:guid}/payouts/{payoutId:guid}/process")]
    public async Task<IActionResult> ProcessPayout(Guid id, Guid payoutId, [FromBody] ProcessPayoutRequest request)
    {
        await _mediator.Send(new ProcessPayoutCommand(id, payoutId, request.TransactionReference));
        return NoContent();
    }

    [HttpPut("{id:guid}/commission-rate")]
    public async Task<IActionResult> SetCommissionRate(Guid id, [FromBody] SetCommissionRateRequest request)
    {
        await _mediator.Send(new SetCustomCommissionRateCommand(id, request.Rate));
        return NoContent();
    }
}

public record ApplyRequest(Guid UserId, string Name, string Email, string? Website = null);
public record AffiliateRejectRequest(string Reason);
public record AffiliateSuspendRequest(string Reason);
public record TrackReferralRequest(string IpAddress, string? UserAgent = null, string? LandingPage = null);
public record ConvertReferralRequest(Guid OrderId, Guid CustomerId);
public record EarnCommissionRequest(Guid OrderId, decimal OrderAmount, string Currency, string Description);
public record PayoutRequest(PayoutMethod Method);
public record ProcessPayoutRequest(string TransactionReference);
public record SetCommissionRateRequest(decimal Rate);
