using EShop.Application.Commands.Bundles;
using EShop.Application.Queries.Bundles;
using EShop.Domain.Aggregates.BundleAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BundlesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BundlesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BundleDto>> Get(Guid id)
    {
        var bundle = await _mediator.Send(new GetBundleQuery(id));
        return bundle != null ? Ok(bundle) : NotFound();
    }

    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<BundleDto>> GetBySku(string sku)
    {
        var bundle = await _mediator.Send(new GetBundleBySkuQuery(sku));
        return bundle != null ? Ok(bundle) : NotFound();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<BundleSummaryDto>>> GetActive()
    {
        var bundles = await _mediator.Send(new GetActiveBundlesQuery());
        return Ok(bundles);
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<IReadOnlyList<BundleSummaryDto>>> GetByType(BundleType type)
    {
        var bundles = await _mediator.Send(new GetBundlesByTypeQuery(type));
        return Ok(bundles);
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<BundleSummaryDto>>> GetByProduct(Guid productId)
    {
        var bundles = await _mediator.Send(new GetBundlesContainingProductQuery(productId));
        return Ok(bundles);
    }

    [HttpPost("fixed")]
    public async Task<ActionResult<Guid>> CreateFixed([FromBody] CreateFixedBundleRequest request)
    {
        var id = await _mediator.Send(new CreateFixedBundleCommand(
            request.Sku, request.Name, request.Description, request.DiscountType, request.DiscountValue, request.ImageUrl));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("customizable")]
    public async Task<ActionResult<Guid>> CreateCustomizable([FromBody] CreateCustomizableBundleRequest request)
    {
        var id = await _mediator.Send(new CreateCustomizableBundleCommand(
            request.Sku, request.Name, request.Description, request.MinItems, request.MaxItems,
            request.DiscountType, request.DiscountValue, request.ImageUrl));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("mix-match")]
    public async Task<ActionResult<Guid>> CreateMixMatch([FromBody] CreateMixMatchBundleRequest request)
    {
        var id = await _mediator.Send(new CreateMixMatchBundleCommand(
            request.Sku, request.Name, request.Description, request.RequiredCount,
            request.DiscountType, request.DiscountValue, request.ImageUrl));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{bundleId:guid}/items")]
    public async Task<ActionResult<Guid>> AddItem(Guid bundleId, [FromBody] AddBundleItemRequest request)
    {
        var itemId = await _mediator.Send(new AddBundleItemCommand(
            bundleId, request.ProductId, request.ProductName, request.ProductSku,
            request.UnitPrice, request.Currency, request.Quantity, request.IsRequired,
            request.MinQuantity, request.MaxQuantity));
        return Ok(itemId);
    }

    [HttpDelete("{bundleId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid bundleId, Guid itemId)
    {
        await _mediator.Send(new RemoveBundleItemCommand(bundleId, itemId));
        return NoContent();
    }

    [HttpPost("{bundleId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid bundleId)
    {
        await _mediator.Send(new ActivateBundleCommand(bundleId));
        return NoContent();
    }

    [HttpPost("{bundleId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid bundleId)
    {
        await _mediator.Send(new DeactivateBundleCommand(bundleId));
        return NoContent();
    }

    [HttpPut("{bundleId:guid}/discount")]
    public async Task<IActionResult> UpdateDiscount(Guid bundleId, [FromBody] UpdateDiscountRequest request)
    {
        await _mediator.Send(new UpdateBundleDiscountCommand(bundleId, request.Type, request.Value));
        return NoContent();
    }

    [HttpPut("{bundleId:guid}/validity")]
    public async Task<IActionResult> SetValidity(Guid bundleId, [FromBody] SetValidityRequest request)
    {
        await _mediator.Send(new SetBundleValidityCommand(bundleId, request.ValidFrom, request.ValidUntil));
        return NoContent();
    }

    [HttpPost("{bundleId:guid}/validate")]
    public async Task<ActionResult<BundleValidationResult>> ValidateSelection(Guid bundleId, [FromBody] ValidateSelectionRequest request)
    {
        var result = await _mediator.Send(new ValidateBundleSelectionQuery(bundleId, request.SelectedQuantities));
        return Ok(result);
    }

    [HttpPost("{bundleId:guid}/calculate-price")]
    public async Task<ActionResult<decimal>> CalculatePrice(Guid bundleId, [FromBody] CalculatePriceRequest? request = null)
    {
        var price = await _mediator.Send(new CalculateBundlePriceQuery(bundleId, request?.SelectedQuantities));
        return Ok(price);
    }
}

// Request DTOs
public record CreateFixedBundleRequest(
    string Sku,
    string Name,
    string Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? ImageUrl = null);

public record CreateCustomizableBundleRequest(
    string Sku,
    string Name,
    string Description,
    int MinItems,
    int MaxItems,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? ImageUrl = null);

public record CreateMixMatchBundleRequest(
    string Sku,
    string Name,
    string Description,
    int RequiredCount,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? ImageUrl = null);

public record AddBundleItemRequest(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    decimal UnitPrice,
    string Currency,
    int Quantity = 1,
    bool IsRequired = true,
    int? MinQuantity = null,
    int? MaxQuantity = null);

public record UpdateDiscountRequest(DiscountType Type, decimal Value);
public record SetValidityRequest(DateTime? ValidFrom, DateTime? ValidUntil);
public record ValidateSelectionRequest(Dictionary<Guid, int> SelectedQuantities);
public record CalculatePriceRequest(Dictionary<Guid, int>? SelectedQuantities);
