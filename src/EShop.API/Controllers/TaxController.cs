using EShop.Application.Commands.Tax;
using EShop.Application.Queries.Tax;
using EShop.Domain.Aggregates.TaxAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaxController : ControllerBase
{
    private readonly IMediator _mediator;

    public TaxController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxConfigurationDto>> Get(Guid id)
    {
        var taxConfig = await _mediator.Send(new GetTaxConfigurationQuery(id));
        return taxConfig != null ? Ok(taxConfig) : NotFound();
    }

    [HttpGet("country/{countryCode}")]
    public async Task<ActionResult<TaxConfigurationDto>> GetByCountry(string countryCode, [FromQuery] string? regionCode = null)
    {
        var taxConfig = await _mediator.Send(new GetTaxConfigurationByCountryQuery(countryCode, regionCode));
        return taxConfig != null ? Ok(taxConfig) : NotFound();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<TaxConfigurationDto>>> GetActive()
    {
        var configs = await _mediator.Send(new GetActiveTaxConfigurationsQuery());
        return Ok(configs);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<TaxCalculationResultDto>> Calculate([FromBody] CalculateTaxRequest request)
    {
        var result = await _mediator.Send(new CalculateTaxQuery(
            request.CountryCode,
            request.Amount,
            request.Currency,
            request.CategoryId,
            request.CustomerId,
            request.IsDigitalGood,
            request.RegionCode));
        return result != null ? Ok(result) : NotFound("Tax configuration not found");
    }

    [HttpPost("vat")]
    public async Task<ActionResult<Guid>> CreateVAT([FromBody] CreateVATRequest request)
    {
        var id = await _mediator.Send(new CreateVATConfigurationCommand(
            request.CountryCode,
            request.Name,
            request.DefaultRate,
            request.IsTaxIncludedInPrice,
            request.RegionCode));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("gst")]
    public async Task<ActionResult<Guid>> CreateGST([FromBody] CreateGSTRequest request)
    {
        var id = await _mediator.Send(new CreateGSTConfigurationCommand(
            request.CountryCode,
            request.Name,
            request.DefaultRate,
            request.IsTaxIncludedInPrice,
            request.RegionCode));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("sales-tax")]
    public async Task<ActionResult<Guid>> CreateSalesTax([FromBody] CreateSalesTaxRequest request)
    {
        var id = await _mediator.Send(new CreateSalesTaxConfigurationCommand(
            request.CountryCode,
            request.RegionCode,
            request.Name,
            request.DefaultRate));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{taxConfigId:guid}/category-rates")]
    public async Task<ActionResult<Guid>> AddCategoryRate(Guid taxConfigId, [FromBody] AddCategoryRateRequest request)
    {
        var id = await _mediator.Send(new AddTaxCategoryRateCommand(
            taxConfigId,
            request.CategoryId,
            request.CategoryName,
            request.Rate,
            request.Description));
        return Ok(id);
    }

    [HttpPost("{taxConfigId:guid}/exemptions")]
    public async Task<ActionResult<Guid>> AddExemption(Guid taxConfigId, [FromBody] AddExemptionRequest request)
    {
        var id = await _mediator.Send(new AddTaxExemptionCommand(
            taxConfigId,
            request.Type,
            request.EntityId,
            request.Description,
            request.ExpiresAt));
        return Ok(id);
    }

    [HttpPut("{taxConfigId:guid}/rate")]
    public async Task<IActionResult> UpdateRate(Guid taxConfigId, [FromBody] UpdateRateRequest request)
    {
        await _mediator.Send(new UpdateTaxDefaultRateCommand(taxConfigId, request.NewRate));
        return NoContent();
    }

    [HttpPut("{taxConfigId:guid}/active")]
    public async Task<IActionResult> SetActive(Guid taxConfigId, [FromBody] SetTaxActiveRequest request)
    {
        await _mediator.Send(new SetTaxConfigurationActiveCommand(taxConfigId, request.IsActive));
        return NoContent();
    }
}

// Request DTOs
public record CalculateTaxRequest(
    string CountryCode,
    decimal Amount,
    string Currency,
    Guid? CategoryId = null,
    Guid? CustomerId = null,
    bool IsDigitalGood = false,
    string? RegionCode = null);

public record CreateVATRequest(
    string CountryCode,
    string Name,
    decimal DefaultRate,
    bool IsTaxIncludedInPrice = true,
    string? RegionCode = null);

public record CreateGSTRequest(
    string CountryCode,
    string Name,
    decimal DefaultRate,
    bool IsTaxIncludedInPrice = false,
    string? RegionCode = null);

public record CreateSalesTaxRequest(
    string CountryCode,
    string RegionCode,
    string Name,
    decimal DefaultRate);

public record AddCategoryRateRequest(
    Guid CategoryId,
    string CategoryName,
    decimal Rate,
    string? Description = null);

public record AddExemptionRequest(
    ExemptionType Type,
    string? EntityId,
    string Description,
    DateTime? ExpiresAt = null);

public record UpdateRateRequest(decimal NewRate);
public record SetTaxActiveRequest(bool IsActive);
