using EShop.Domain.Aggregates.CurrencyAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Queries.Currency;

// Get Currency by ID
public record GetCurrencyQuery(Guid CurrencyId) : IRequest<CurrencyDto?>;

public class GetCurrencyQueryHandler : IRequestHandler<GetCurrencyQuery, CurrencyDto?>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public GetCurrencyQueryHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<CurrencyDto?> Handle(GetCurrencyQuery request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByIdAsync(request.CurrencyId, cancellationToken);
        return currency != null ? CurrencyDto.FromCurrency(currency) : null;
    }
}

// Get Currency by Code
public record GetCurrencyByCodeQuery(string Code) : IRequest<CurrencyDto?>;

public class GetCurrencyByCodeQueryHandler : IRequestHandler<GetCurrencyByCodeQuery, CurrencyDto?>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public GetCurrencyByCodeQueryHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<CurrencyDto?> Handle(GetCurrencyByCodeQuery request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        return currency != null ? CurrencyDto.FromCurrency(currency) : null;
    }
}

// Get Primary Currency
public record GetPrimaryCurrencyQuery : IRequest<CurrencyDto?>;

public class GetPrimaryCurrencyQueryHandler : IRequestHandler<GetPrimaryCurrencyQuery, CurrencyDto?>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public GetPrimaryCurrencyQueryHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<CurrencyDto?> Handle(GetPrimaryCurrencyQuery request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetPrimaryAsync(cancellationToken);
        return currency != null ? CurrencyDto.FromCurrency(currency) : null;
    }
}

// Get All Active Currencies
public record GetActiveCurrenciesQuery : IRequest<IReadOnlyList<CurrencyDto>>;

public class GetActiveCurrenciesQueryHandler : IRequestHandler<GetActiveCurrenciesQuery, IReadOnlyList<CurrencyDto>>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public GetActiveCurrenciesQueryHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<CurrencyDto>> Handle(GetActiveCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var currencies = await _repository.GetAllActiveAsync(cancellationToken);
        return currencies.Select(CurrencyDto.FromCurrency).ToList();
    }
}

// Convert Currency
public record ConvertCurrencyQuery(string FromCurrencyCode, string ToCurrencyCode, decimal Amount) : IRequest<CurrencyConversionResultDto?>;

public class ConvertCurrencyQueryHandler : IRequestHandler<ConvertCurrencyQuery, CurrencyConversionResultDto?>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public ConvertCurrencyQueryHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<CurrencyConversionResultDto?> Handle(ConvertCurrencyQuery request, CancellationToken cancellationToken)
    {
        var fromCurrency = await _repository.GetByCodeAsync(request.FromCurrencyCode, cancellationToken);
        if (fromCurrency == null) return null;

        var rate = fromCurrency.GetCurrentRate(request.ToCurrencyCode);
        if (rate == null) return null;

        var converted = fromCurrency.Convert(new Money(request.Amount, request.FromCurrencyCode), request.ToCurrencyCode);

        return new CurrencyConversionResultDto(
            request.FromCurrencyCode,
            request.ToCurrencyCode,
            request.Amount,
            converted.Amount,
            rate.Rate,
            rate.Source.ToString());
    }
}

// Format Amount
public record FormatAmountQuery(string CurrencyCode, decimal Amount) : IRequest<string?>;

public class FormatAmountQueryHandler : IRequestHandler<FormatAmountQuery, string?>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public FormatAmountQueryHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<string?> Handle(FormatAmountQuery request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByCodeAsync(request.CurrencyCode, cancellationToken);
        return currency?.Format(request.Amount);
    }
}

// DTOs
public record CurrencyDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    string? NativeSymbol,
    int DecimalPlaces,
    string DecimalSeparator,
    string ThousandsSeparator,
    bool SymbolFirst,
    bool IsActive,
    bool IsPrimary,
    RoundingRule RoundingRule,
    IReadOnlyList<ExchangeRateDto> ExchangeRates)
{
    public static CurrencyDto FromCurrency(CurrencyConfiguration c) => new(
        c.Id,
        c.Code,
        c.Name,
        c.Symbol,
        c.NativeSymbol,
        c.DecimalPlaces,
        c.DecimalSeparator,
        c.ThousandsSeparator,
        c.SymbolFirst,
        c.IsActive,
        c.IsPrimary,
        c.RoundingRule,
        c.ExchangeRates.Where(e => e.IsActive).Select(e => new ExchangeRateDto(
            e.Id,
            e.TargetCurrencyCode,
            e.Rate,
            e.InverseRate,
            e.Source,
            e.EffectiveFrom)).ToList());
}

public record ExchangeRateDto(
    Guid Id,
    string TargetCurrencyCode,
    decimal Rate,
    decimal InverseRate,
    ExchangeRateSource Source,
    DateTime EffectiveFrom);

public record CurrencyConversionResultDto(
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal OriginalAmount,
    decimal ConvertedAmount,
    decimal ExchangeRate,
    string RateSource);
