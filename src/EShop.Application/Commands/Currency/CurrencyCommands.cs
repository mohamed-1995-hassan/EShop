using EShop.Domain.Aggregates.CurrencyAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Currency;

// Create Currency Configuration
public record CreateCurrencyConfigurationCommand(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces = 2,
    bool SymbolFirst = true,
    string? NativeSymbol = null) : IRequest<Guid>;

public class CreateCurrencyConfigurationCommandHandler : IRequestHandler<CreateCurrencyConfigurationCommand, Guid>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public CreateCurrencyConfigurationCommandHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateCurrencyConfigurationCommand request, CancellationToken cancellationToken)
    {
        var currency = CurrencyConfiguration.Create(
            request.Code,
            request.Name,
            request.Symbol,
            request.DecimalPlaces,
            request.SymbolFirst,
            request.NativeSymbol);

        await _repository.AddAsync(currency, cancellationToken);
        return currency.Id;
    }
}

// Set Exchange Rate
public record SetExchangeRateCommand(
    Guid CurrencyId,
    string TargetCurrencyCode,
    decimal Rate,
    ExchangeRateSource Source = ExchangeRateSource.Manual) : IRequest<Guid>;

public class SetExchangeRateCommandHandler : IRequestHandler<SetExchangeRateCommand, Guid>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public SetExchangeRateCommandHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task<Guid> Handle(SetExchangeRateCommand request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByIdAsync(request.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException("Currency not found");

        var rate = currency.SetExchangeRate(request.TargetCurrencyCode, request.Rate, request.Source);
        await _repository.UpdateAsync(currency, cancellationToken);
        return rate.Id;
    }
}

// Set Primary Currency
public record SetPrimaryCurrencyCommand(Guid CurrencyId) : IRequest;

public class SetPrimaryCurrencyCommandHandler : IRequestHandler<SetPrimaryCurrencyCommand>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public SetPrimaryCurrencyCommandHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task Handle(SetPrimaryCurrencyCommand request, CancellationToken cancellationToken)
    {
        // Clear current primary
        var currentPrimary = await _repository.GetPrimaryAsync(cancellationToken);
        if (currentPrimary != null)
        {
            currentPrimary.ClearPrimary();
            await _repository.UpdateAsync(currentPrimary, cancellationToken);
        }

        // Set new primary
        var currency = await _repository.GetByIdAsync(request.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException("Currency not found");

        currency.SetAsPrimary();
        await _repository.UpdateAsync(currency, cancellationToken);
    }
}

// Set Formatting
public record SetCurrencyFormattingCommand(
    Guid CurrencyId,
    string DecimalSeparator,
    string ThousandsSeparator,
    bool SymbolFirst) : IRequest;

public class SetCurrencyFormattingCommandHandler : IRequestHandler<SetCurrencyFormattingCommand>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public SetCurrencyFormattingCommandHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task Handle(SetCurrencyFormattingCommand request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByIdAsync(request.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException("Currency not found");

        currency.SetFormatting(request.DecimalSeparator, request.ThousandsSeparator, request.SymbolFirst);
        await _repository.UpdateAsync(currency, cancellationToken);
    }
}

// Set Rounding Rule
public record SetCurrencyRoundingRuleCommand(Guid CurrencyId, RoundingRule Rule) : IRequest;

public class SetCurrencyRoundingRuleCommandHandler : IRequestHandler<SetCurrencyRoundingRuleCommand>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public SetCurrencyRoundingRuleCommandHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task Handle(SetCurrencyRoundingRuleCommand request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByIdAsync(request.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException("Currency not found");

        currency.SetRoundingRule(request.Rule);
        await _repository.UpdateAsync(currency, cancellationToken);
    }
}

// Activate/Deactivate Currency
public record SetCurrencyActiveCommand(Guid CurrencyId, bool IsActive) : IRequest;

public class SetCurrencyActiveCommandHandler : IRequestHandler<SetCurrencyActiveCommand>
{
    private readonly ICurrencyConfigurationRepository _repository;

    public SetCurrencyActiveCommandHandler(ICurrencyConfigurationRepository repository) => _repository = repository;

    public async Task Handle(SetCurrencyActiveCommand request, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByIdAsync(request.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException("Currency not found");

        if (request.IsActive)
            currency.Activate();
        else
            currency.Deactivate();

        await _repository.UpdateAsync(currency, cancellationToken);
    }
}
