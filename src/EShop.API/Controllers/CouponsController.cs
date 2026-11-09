using EShop.Application.Commands.Coupons;
using EShop.Application.Queries.Coupons;
using EShop.Domain.Aggregates.CouponAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CouponsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CouponDto>> Get(Guid id)
    {
        var coupon = await _mediator.Send(new GetCouponQuery(id));
        return coupon != null ? Ok(coupon) : NotFound();
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<CouponDto>> GetByCode(string code)
    {
        var coupon = await _mediator.Send(new GetCouponByCodeQuery(code));
        return coupon != null ? Ok(coupon) : NotFound();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<CouponSummaryDto>>> GetActive()
    {
        var coupons = await _mediator.Send(new GetActiveCouponsQuery());
        return Ok(coupons);
    }

    [HttpGet("expiring-soon")]
    public async Task<ActionResult<IReadOnlyList<CouponSummaryDto>>> GetExpiringSoon([FromQuery] int withinDays = 7)
    {
        var coupons = await _mediator.Send(new GetExpiringSoonCouponsQuery(withinDays));
        return Ok(coupons);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<CouponValidationResult>> Validate([FromBody] ValidateCouponRequest request)
    {
        var result = await _mediator.Send(new ValidateCouponQuery(request.Code, request.CustomerId, request.OrderTotal, request.Currency, request.IsFirstOrder));
        return Ok(result);
    }

    [HttpPost("percentage")]
    public async Task<ActionResult<Guid>> CreatePercentage([FromBody] CreatePercentageCouponRequest request)
    {
        var id = await _mediator.Send(new CreatePercentageCouponCommand(request.Code, request.Name, request.Percentage, request.MaxDiscount, request.MaxUsage));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("fixed-amount")]
    public async Task<ActionResult<Guid>> CreateFixedAmount([FromBody] CreateFixedAmountCouponRequest request)
    {
        var id = await _mediator.Send(new CreateFixedAmountCouponCommand(request.Code, request.Name, request.Amount, request.Currency, request.MaxUsage));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("free-shipping")]
    public async Task<ActionResult<Guid>> CreateFreeShipping([FromBody] CreateFreeShippingCouponRequest request)
    {
        var id = await _mediator.Send(new CreateFreeShippingCouponCommand(request.Code, request.Name, request.MaxUsage));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GenerateBulk([FromBody] GenerateBulkCouponsRequest request)
    {
        var ids = await _mediator.Send(new GenerateBulkCouponsCommand(request.Prefix, request.Name, request.DiscountType, request.DiscountValue, request.Quantity, request.UsagePerCoupon));
        return Ok(ids);
    }

    [HttpPost("{id:guid}/apply")]
    public async Task<ActionResult<Guid>> Apply(Guid id, [FromBody] ApplyCouponRequest request)
    {
        var usageId = await _mediator.Send(new ApplyCouponCommand(id, request.OrderId, request.CustomerId, request.DiscountApplied, request.Currency));
        return Ok(usageId);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _mediator.Send(new ActivateCouponCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _mediator.Send(new DeactivateCouponCommand(id));
        return NoContent();
    }

    [HttpPut("{id:guid}/validity")]
    public async Task<IActionResult> SetValidity(Guid id, [FromBody] CouponSetValidityRequest request)
    {
        await _mediator.Send(new SetCouponValidityCommand(id, request.ValidFrom, request.ValidUntil));
        return NoContent();
    }

    [HttpPut("{id:guid}/min-order")]
    public async Task<IActionResult> SetMinOrder(Guid id, [FromBody] SetMinOrderRequest request)
    {
        await _mediator.Send(new SetCouponMinOrderCommand(id, request.MinimumAmount, request.Currency));
        return NoContent();
    }
}

public record ValidateCouponRequest(string Code, Guid CustomerId, decimal OrderTotal, string Currency, bool IsFirstOrder = false);
public record CreatePercentageCouponRequest(string Code, string Name, decimal Percentage, decimal? MaxDiscount = null, int? MaxUsage = null);
public record CreateFixedAmountCouponRequest(string Code, string Name, decimal Amount, string Currency, int? MaxUsage = null);
public record CreateFreeShippingCouponRequest(string Code, string Name, int? MaxUsage = null);
public record GenerateBulkCouponsRequest(string Prefix, string Name, DiscountType DiscountType, decimal DiscountValue, int Quantity, int UsagePerCoupon = 1);
public record ApplyCouponRequest(Guid OrderId, Guid CustomerId, decimal DiscountApplied, string Currency);
public record CouponSetValidityRequest(DateTime? ValidFrom, DateTime? ValidUntil);
public record SetMinOrderRequest(decimal MinimumAmount, string Currency);
