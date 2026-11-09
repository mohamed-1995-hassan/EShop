using EShop.Application.Commands.Promotions;
using EShop.Application.Queries.Promotions;
using EShop.Domain.Aggregates.PromotionAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PromotionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromotionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get promotion by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PromotionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionDetailsDto>> GetPromotion(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPromotionQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Validate promo code
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(PromoValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromoValidationResult>> ValidatePromoCode(
        [FromBody] ValidatePromoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ValidatePromoCodeCommand(
                request.Code,
                request.CustomerId,
                request.OrderTotal,
                request.CustomerTier,
                request.ProductIds),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Create a percentage discount promotion (Admin)
    /// </summary>
    [HttpPost("percentage")]
    [ProducesResponseType(typeof(CreatePromotionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePromotionResult>> CreatePercentagePromotion(
        [FromBody] CreatePercentagePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePercentagePromotionCommand(
            request.Code,
            request.Name,
            request.Percentage,
            request.StartDate,
            request.EndDate,
            request.MaxDiscountAmount,
            request.MinimumOrderValue,
            request.MaxUsageTotal,
            request.MaxUsagePerCustomer);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetPromotion), new { id = result.PromotionId }, result);
    }

    /// <summary>
    /// Create a fixed discount promotion (Admin)
    /// </summary>
    [HttpPost("fixed")]
    [ProducesResponseType(typeof(CreatePromotionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePromotionResult>> CreateFixedDiscountPromotion(
        [FromBody] CreateFixedDiscountPromotionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFixedDiscountPromotionCommand(
            request.Code,
            request.Name,
            request.Amount,
            request.Currency ?? "KWD",
            request.StartDate,
            request.EndDate,
            request.MinimumOrderValue,
            request.MaxUsageTotal,
            request.MaxUsagePerCustomer);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetPromotion), new { id = result.PromotionId }, result);
    }

    /// <summary>
    /// Create a free shipping promotion (Admin)
    /// </summary>
    [HttpPost("free-shipping")]
    [ProducesResponseType(typeof(CreatePromotionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePromotionResult>> CreateFreeShippingPromotion(
        [FromBody] CreateFreeShippingPromotionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFreeShippingPromotionCommand(
            request.Code,
            request.Name,
            request.StartDate,
            request.EndDate,
            request.MinimumOrderValue);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetPromotion), new { id = result.PromotionId }, result);
    }

    /// <summary>
    /// Activate promotion (Admin)
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(PromotionOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionOperationResult>> ActivatePromotion(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ActivatePromotionCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Deactivate promotion (Admin)
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(PromotionOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionOperationResult>> DeactivatePromotion(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivatePromotionCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Extend promotion period (Admin)
    /// </summary>
    [HttpPost("{id:guid}/extend")]
    [ProducesResponseType(typeof(PromotionOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionOperationResult>> ExtendPromotion(
        Guid id,
        [FromBody] ExtendPromotionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExtendPromotionCommand(id, request.NewEndDate), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Add applicable product to promotion (Admin)
    /// </summary>
    [HttpPost("{id:guid}/products/{productId:guid}")]
    [ProducesResponseType(typeof(PromotionOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionOperationResult>> AddApplicableProduct(
        Guid id,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddApplicableProductCommand(id, productId), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Record promo usage (internal)
    /// </summary>
    [HttpPost("{id:guid}/usage")]
    [ProducesResponseType(typeof(PromotionOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionOperationResult>> RecordUsage(
        Guid id,
        [FromBody] RecordUsageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RecordPromotionUsageCommand(id, request.CustomerId, request.OrderId, request.DiscountApplied), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Get active promotions
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<PromotionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromotionSummaryDto>>> GetActivePromotions(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActivePromotionsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get promotions by type (Admin)
    /// </summary>
    [HttpGet("type/{type}")]
    [ProducesResponseType(typeof(List<PromotionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromotionSummaryDto>>> GetPromotionsByType(
        PromotionType type,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPromotionsByTypeQuery(type), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get upcoming promotions (Admin)
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(List<PromotionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromotionSummaryDto>>> GetUpcomingPromotions(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUpcomingPromotionsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get promotions for product
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(List<PromotionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromotionSummaryDto>>> GetProductPromotions(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductPromotionsQuery(productId), cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record CreatePercentagePromotionRequest(
    string Code,
    string Name,
    decimal Percentage,
    DateTime StartDate,
    DateTime EndDate,
    decimal? MaxDiscountAmount = null,
    decimal? MinimumOrderValue = null,
    int? MaxUsageTotal = null,
    int? MaxUsagePerCustomer = null);

public record CreateFixedDiscountPromotionRequest(
    string Code,
    string Name,
    decimal Amount,
    string? Currency,
    DateTime StartDate,
    DateTime EndDate,
    decimal? MinimumOrderValue = null,
    int? MaxUsageTotal = null,
    int? MaxUsagePerCustomer = null);

public record CreateFreeShippingPromotionRequest(
    string Code,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal? MinimumOrderValue = null);

public record ValidatePromoRequest(
    string Code,
    Guid CustomerId,
    decimal OrderTotal,
    int CustomerTier = 1,
    List<Guid>? ProductIds = null);

public record ExtendPromotionRequest(DateTime NewEndDate);

public record RecordUsageRequest(
    Guid CustomerId,
    Guid OrderId,
    decimal DiscountApplied);
