using EShop.Domain.Aggregates.TaxAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Queries.Tax;

// Get Tax Configuration by ID
public record GetTaxConfigurationQuery(Guid TaxConfigId) : IRequest<TaxConfigurationDto?>;

public class GetTaxConfigurationQueryHandler : IRequestHandler<GetTaxConfigurationQuery, TaxConfigurationDto?>
{
    private readonly ITaxConfigurationRepository _repository;

    public GetTaxConfigurationQueryHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<TaxConfigurationDto?> Handle(GetTaxConfigurationQuery request, CancellationToken cancellationToken)
    {
        var taxConfig = await _repository.GetByIdAsync(request.TaxConfigId, cancellationToken);
        return taxConfig != null ? TaxConfigurationDto.FromTaxConfiguration(taxConfig) : null;
    }
}

// Get Tax Configuration by Country
public record GetTaxConfigurationByCountryQuery(string CountryCode, string? RegionCode = null) : IRequest<TaxConfigurationDto?>;

public class GetTaxConfigurationByCountryQueryHandler : IRequestHandler<GetTaxConfigurationByCountryQuery, TaxConfigurationDto?>
{
    private readonly ITaxConfigurationRepository _repository;

    public GetTaxConfigurationByCountryQueryHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<TaxConfigurationDto?> Handle(GetTaxConfigurationByCountryQuery request, CancellationToken cancellationToken)
    {
        var taxConfig = await _repository.GetByCountryAsync(request.CountryCode, request.RegionCode, cancellationToken);
        return taxConfig != null ? TaxConfigurationDto.FromTaxConfiguration(taxConfig) : null;
    }
}

// Get All Active Tax Configurations
public record GetActiveTaxConfigurationsQuery : IRequest<IReadOnlyList<TaxConfigurationDto>>;

public class GetActiveTaxConfigurationsQueryHandler : IRequestHandler<GetActiveTaxConfigurationsQuery, IReadOnlyList<TaxConfigurationDto>>
{
    private readonly ITaxConfigurationRepository _repository;

    public GetActiveTaxConfigurationsQueryHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<TaxConfigurationDto>> Handle(GetActiveTaxConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _repository.GetActiveConfigurationsAsync(cancellationToken);
        return configs.Select(TaxConfigurationDto.FromTaxConfiguration).ToList();
    }
}

// Calculate Tax
public record CalculateTaxQuery(
    string CountryCode,
    decimal Amount,
    string Currency,
    Guid? CategoryId = null,
    Guid? CustomerId = null,
    bool IsDigitalGood = false,
    string? RegionCode = null) : IRequest<TaxCalculationResultDto?>;

public class CalculateTaxQueryHandler : IRequestHandler<CalculateTaxQuery, TaxCalculationResultDto?>
{
    private readonly ITaxConfigurationRepository _repository;

    public CalculateTaxQueryHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<TaxCalculationResultDto?> Handle(CalculateTaxQuery request, CancellationToken cancellationToken)
    {
        var taxConfig = await _repository.GetByCountryAsync(request.CountryCode, request.RegionCode, cancellationToken);
        if (taxConfig == null) return null;

        var result = taxConfig.CalculateTax(
            new Money(request.Amount, request.Currency),
            request.CategoryId,
            request.CustomerId,
            request.IsDigitalGood);

        return new TaxCalculationResultDto(
            result.NetAmount.Amount,
            result.TaxAmount.Amount,
            result.GrossAmount.Amount,
            result.GrossAmount.Currency,
            result.AppliedRate,
            result.TaxType.ToString(),
            result.TaxName,
            result.IsExempt,
            result.ExemptionReason);
    }
}

// DTOs
public record TaxConfigurationDto(
    Guid Id,
    string CountryCode,
    string? RegionCode,
    string Name,
    TaxType TaxType,
    decimal DefaultRate,
    bool IsTaxIncludedInPrice,
    bool IsActive,
    decimal? TaxFreeThreshold,
    DateTime EffectiveFrom,
    DateTime? EffectiveUntil,
    IReadOnlyList<TaxCategoryRateDto> CategoryRates,
    IReadOnlyList<TaxExemptionDto> Exemptions)
{
    public static TaxConfigurationDto FromTaxConfiguration(TaxConfiguration t) => new(
        t.Id,
        t.CountryCode,
        t.RegionCode,
        t.Name,
        t.TaxType,
        t.DefaultRate,
        t.IsTaxIncludedInPrice,
        t.IsActive,
        t.TaxFreeThreshold,
        t.EffectiveFrom,
        t.EffectiveUntil,
        t.CategoryRates.Select(c => new TaxCategoryRateDto(
            c.Id,
            c.CategoryId,
            c.CategoryName,
            c.Rate,
            c.Description)).ToList(),
        t.Exemptions.Select(e => new TaxExemptionDto(
            e.Id,
            e.Type,
            e.EntityId,
            e.Description,
            e.ExpiresAt,
            e.IsExpired)).ToList());
}

public record TaxCategoryRateDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal Rate,
    string? Description);

public record TaxExemptionDto(
    Guid Id,
    ExemptionType Type,
    string? EntityId,
    string Description,
    DateTime? ExpiresAt,
    bool IsExpired);

public record TaxCalculationResultDto(
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount,
    string Currency,
    decimal AppliedRate,
    string TaxType,
    string TaxName,
    bool IsExempt,
    string? ExemptionReason);
