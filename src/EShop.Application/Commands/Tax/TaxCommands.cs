using EShop.Domain.Aggregates.TaxAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Tax;

// Create VAT Configuration
public record CreateVATConfigurationCommand(
    string CountryCode,
    string Name,
    decimal DefaultRate,
    bool IsTaxIncludedInPrice = true,
    string? RegionCode = null) : IRequest<Guid>;

public class CreateVATConfigurationCommandHandler : IRequestHandler<CreateVATConfigurationCommand, Guid>
{
    private readonly ITaxConfigurationRepository _repository;

    public CreateVATConfigurationCommandHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateVATConfigurationCommand request, CancellationToken cancellationToken)
    {
        var taxConfig = TaxConfiguration.CreateVAT(
            request.CountryCode,
            request.Name,
            request.DefaultRate,
            request.IsTaxIncludedInPrice,
            request.RegionCode);

        await _repository.AddAsync(taxConfig, cancellationToken);
        return taxConfig.Id;
    }
}

// Create GST Configuration
public record CreateGSTConfigurationCommand(
    string CountryCode,
    string Name,
    decimal DefaultRate,
    bool IsTaxIncludedInPrice = false,
    string? RegionCode = null) : IRequest<Guid>;

public class CreateGSTConfigurationCommandHandler : IRequestHandler<CreateGSTConfigurationCommand, Guid>
{
    private readonly ITaxConfigurationRepository _repository;

    public CreateGSTConfigurationCommandHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateGSTConfigurationCommand request, CancellationToken cancellationToken)
    {
        var taxConfig = TaxConfiguration.CreateGST(
            request.CountryCode,
            request.Name,
            request.DefaultRate,
            request.IsTaxIncludedInPrice,
            request.RegionCode);

        await _repository.AddAsync(taxConfig, cancellationToken);
        return taxConfig.Id;
    }
}

// Create Sales Tax Configuration
public record CreateSalesTaxConfigurationCommand(
    string CountryCode,
    string RegionCode,
    string Name,
    decimal DefaultRate) : IRequest<Guid>;

public class CreateSalesTaxConfigurationCommandHandler : IRequestHandler<CreateSalesTaxConfigurationCommand, Guid>
{
    private readonly ITaxConfigurationRepository _repository;

    public CreateSalesTaxConfigurationCommandHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateSalesTaxConfigurationCommand request, CancellationToken cancellationToken)
    {
        var taxConfig = TaxConfiguration.CreateSalesTax(
            request.CountryCode,
            request.RegionCode,
            request.Name,
            request.DefaultRate);

        await _repository.AddAsync(taxConfig, cancellationToken);
        return taxConfig.Id;
    }
}

// Add Category Rate
public record AddTaxCategoryRateCommand(
    Guid TaxConfigId,
    Guid CategoryId,
    string CategoryName,
    decimal Rate,
    string? Description = null) : IRequest<Guid>;

public class AddTaxCategoryRateCommandHandler : IRequestHandler<AddTaxCategoryRateCommand, Guid>
{
    private readonly ITaxConfigurationRepository _repository;

    public AddTaxCategoryRateCommandHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddTaxCategoryRateCommand request, CancellationToken cancellationToken)
    {
        var taxConfig = await _repository.GetByIdAsync(request.TaxConfigId, cancellationToken)
            ?? throw new InvalidOperationException("Tax configuration not found");

        var categoryRate = taxConfig.AddCategoryRate(
            request.CategoryId,
            request.CategoryName,
            request.Rate,
            request.Description);

        await _repository.UpdateAsync(taxConfig, cancellationToken);
        return categoryRate.Id;
    }
}

// Add Tax Exemption
public record AddTaxExemptionCommand(
    Guid TaxConfigId,
    ExemptionType Type,
    string? EntityId,
    string Description,
    DateTime? ExpiresAt = null) : IRequest<Guid>;

public class AddTaxExemptionCommandHandler : IRequestHandler<AddTaxExemptionCommand, Guid>
{
    private readonly ITaxConfigurationRepository _repository;

    public AddTaxExemptionCommandHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddTaxExemptionCommand request, CancellationToken cancellationToken)
    {
        var taxConfig = await _repository.GetByIdAsync(request.TaxConfigId, cancellationToken)
            ?? throw new InvalidOperationException("Tax configuration not found");

        var exemption = taxConfig.AddExemption(
            request.Type,
            request.EntityId,
            request.Description,
            request.ExpiresAt);

        await _repository.UpdateAsync(taxConfig, cancellationToken);
        return exemption.Id;
    }
}

// Update Default Rate
public record UpdateTaxDefaultRateCommand(Guid TaxConfigId, decimal NewRate) : IRequest;

public class UpdateTaxDefaultRateCommandHandler : IRequestHandler<UpdateTaxDefaultRateCommand>
{
    private readonly ITaxConfigurationRepository _repository;

    public UpdateTaxDefaultRateCommandHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task Handle(UpdateTaxDefaultRateCommand request, CancellationToken cancellationToken)
    {
        var taxConfig = await _repository.GetByIdAsync(request.TaxConfigId, cancellationToken)
            ?? throw new InvalidOperationException("Tax configuration not found");

        taxConfig.UpdateDefaultRate(request.NewRate);
        await _repository.UpdateAsync(taxConfig, cancellationToken);
    }
}

// Activate/Deactivate Tax Configuration
public record SetTaxConfigurationActiveCommand(Guid TaxConfigId, bool IsActive) : IRequest;

public class SetTaxConfigurationActiveCommandHandler : IRequestHandler<SetTaxConfigurationActiveCommand>
{
    private readonly ITaxConfigurationRepository _repository;

    public SetTaxConfigurationActiveCommandHandler(ITaxConfigurationRepository repository) => _repository = repository;

    public async Task Handle(SetTaxConfigurationActiveCommand request, CancellationToken cancellationToken)
    {
        var taxConfig = await _repository.GetByIdAsync(request.TaxConfigId, cancellationToken)
            ?? throw new InvalidOperationException("Tax configuration not found");

        if (request.IsActive)
            taxConfig.Activate();
        else
            taxConfig.Deactivate();

        await _repository.UpdateAsync(taxConfig, cancellationToken);
    }
}
