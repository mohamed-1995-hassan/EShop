using EShop.Application.Commands.Currency;
using EShop.Application.Queries.Currency;
using EShop.Domain.Aggregates.CurrencyAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurrencyController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CurrencyDto>> Get(Guid id)
    {
        var currency = await _mediator.Send(new GetCurrencyQuery(id));
        return currency != null ? Ok(currency) : NotFound();
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<CurrencyDto>> GetByCode(string code)
    {
        var currency = await _mediator.Send(new GetCurrencyByCodeQuery(code));
        return currency != null ? Ok(currency) : NotFound();
    }

    [HttpGet("primary")]
    public async Task<ActionResult<CurrencyDto>> GetPrimary()
    {
        var currency = await _mediator.Send(new GetPrimaryCurrencyQuery());
        return currency != null ? Ok(currency) : NotFound();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> GetActive()
    {
        var currencies = await _mediator.Send(new GetActiveCurrenciesQuery());
        return Ok(currencies);
    }

    [HttpGet("convert")]
    public async Task<ActionResult<CurrencyConversionResultDto>> Convert(
        [FromQuery] string from, 
        [FromQuery] string to, 
        [FromQuery] decimal amount)
    {
        var result = await _mediator.Send(new ConvertCurrencyQuery(from, to, amount));
        return result != null ? Ok(result) : NotFound("Exchange rate not found");
    }

    [HttpGet("format")]
    public async Task<ActionResult<string>> Format([FromQuery] string code, [FromQuery] decimal amount)
    {
        var formatted = await _mediator.Send(new FormatAmountQuery(code, amount));
        return formatted != null ? Ok(formatted) : NotFound("Currency not found");
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCurrencyRequest request)
    {
        var id = await _mediator.Send(new CreateCurrencyConfigurationCommand(
            request.Code,
            request.Name,
            request.Symbol,
            request.DecimalPlaces,
            request.SymbolFirst,
            request.NativeSymbol));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{currencyId:guid}/exchange-rates")]
    public async Task<ActionResult<Guid>> SetExchangeRate(Guid currencyId, [FromBody] SetExchangeRateRequest request)
    {
        var id = await _mediator.Send(new SetExchangeRateCommand(
            currencyId,
            request.TargetCurrencyCode,
            request.Rate,
            request.Source));
        return Ok(id);
    }

    [HttpPost("{currencyId:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid currencyId)
    {
        await _mediator.Send(new SetPrimaryCurrencyCommand(currencyId));
        return NoContent();
    }

    [HttpPut("{currencyId:guid}/formatting")]
    public async Task<IActionResult> SetFormatting(Guid currencyId, [FromBody] SetFormattingRequest request)
    {
        await _mediator.Send(new SetCurrencyFormattingCommand(
            currencyId,
            request.DecimalSeparator,
            request.ThousandsSeparator,
            request.SymbolFirst));
        return NoContent();
    }

    [HttpPut("{currencyId:guid}/rounding")]
    public async Task<IActionResult> SetRoundingRule(Guid currencyId, [FromBody] SetRoundingRuleRequest request)
    {
        await _mediator.Send(new SetCurrencyRoundingRuleCommand(currencyId, request.Rule));
        return NoContent();
    }

    [HttpPut("{currencyId:guid}/active")]
    public async Task<IActionResult> SetActive(Guid currencyId, [FromBody] SetActiveRequest request)
    {
        await _mediator.Send(new SetCurrencyActiveCommand(currencyId, request.IsActive));
        return NoContent();
    }
}

// Request DTOs
public record CreateCurrencyRequest(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces = 2,
    bool SymbolFirst = true,
    string? NativeSymbol = null);

public record SetExchangeRateRequest(
    string TargetCurrencyCode,
    decimal Rate,
    ExchangeRateSource Source = ExchangeRateSource.Manual);

public record SetFormattingRequest(
    string DecimalSeparator,
    string ThousandsSeparator,
    bool SymbolFirst);

public record SetRoundingRuleRequest(RoundingRule Rule);
public record SetActiveRequest(bool IsActive);
