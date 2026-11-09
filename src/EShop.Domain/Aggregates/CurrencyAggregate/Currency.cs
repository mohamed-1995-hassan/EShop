using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.CurrencyAggregate;

/// <summary>
/// Currency Configuration Aggregate - Multi-currency support
/// 
/// BUSINESS RULES:
/// - Exchange rates updated regularly
/// - Historical rates preserved for reporting
/// - Rounding rules per currency
/// - Primary/secondary currencies configured
/// - Currency-specific formatting
/// </summary>
public class CurrencyConfiguration : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public string? NativeSymbol { get; private set; }
    public int DecimalPlaces { get; private set; }
    public string DecimalSeparator { get; private set; } = ".";
    public string ThousandsSeparator { get; private set; } = ",";
    public bool SymbolFirst { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPrimary { get; private set; }
    public RoundingRule RoundingRule { get; private set; }
    public decimal? MinimumAmount { get; private set; }
    public decimal? MaximumAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<ExchangeRate> _exchangeRates = new();
    public IReadOnlyCollection<ExchangeRate> ExchangeRates => _exchangeRates.AsReadOnly();

    private CurrencyConfiguration() { }

    public static CurrencyConfiguration Create(
        string code,
        string name,
        string symbol,
        int decimalPlaces = 2,
        bool symbolFirst = true,
        string? nativeSymbol = null)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new DomainException("Currency code must be 3 characters (ISO 4217)");

        return new CurrencyConfiguration
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            Name = name,
            Symbol = symbol,
            NativeSymbol = nativeSymbol ?? symbol,
            DecimalPlaces = decimalPlaces,
            SymbolFirst = symbolFirst,
            IsActive = true,
            IsPrimary = false,
            RoundingRule = RoundingRule.Standard,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static CurrencyConfiguration CreateKWD()
    {
        var currency = Create("KWD", "Kuwaiti Dinar", "KD", 3, true, "د.ك");
        currency.IsPrimary = true;
        return currency;
    }

    public static CurrencyConfiguration CreateUSD()
    {
        return Create("USD", "US Dollar", "$", 2, true);
    }

    public static CurrencyConfiguration CreateEUR()
    {
        return Create("EUR", "Euro", "€", 2, false);
    }

    public static CurrencyConfiguration CreateGBP()
    {
        return Create("GBP", "British Pound", "£", 2, true);
    }

    public static CurrencyConfiguration CreateJPY()
    {
        var currency = Create("JPY", "Japanese Yen", "¥", 0, true);
        currency.RoundingRule = RoundingRule.NoDecimal;
        return currency;
    }

    #region Exchange Rates

    public ExchangeRate SetExchangeRate(
        string targetCurrencyCode,
        decimal rate,
        ExchangeRateSource source = ExchangeRateSource.Manual)
    {
        if (rate <= 0)
            throw new DomainException("Exchange rate must be greater than zero");

        // Expire existing rate
        var existingRate = _exchangeRates
            .FirstOrDefault(r => r.TargetCurrencyCode == targetCurrencyCode && r.IsActive);
        existingRate?.Expire();

        var exchangeRate = new ExchangeRate(
            Guid.NewGuid(),
            targetCurrencyCode.ToUpperInvariant(),
            rate,
            source);

        _exchangeRates.Add(exchangeRate);
        return exchangeRate;
    }

    public ExchangeRate? GetCurrentRate(string targetCurrencyCode)
    {
        return _exchangeRates
            .FirstOrDefault(r => r.TargetCurrencyCode == targetCurrencyCode && r.IsActive);
    }

    public IEnumerable<ExchangeRate> GetHistoricalRates(string targetCurrencyCode, DateTime from, DateTime to)
    {
        return _exchangeRates
            .Where(r => r.TargetCurrencyCode == targetCurrencyCode &&
                        r.EffectiveFrom >= from &&
                        r.EffectiveFrom <= to)
            .OrderByDescending(r => r.EffectiveFrom);
    }

    public Money Convert(Money amount, string targetCurrencyCode)
    {
        if (amount.Currency == targetCurrencyCode)
            return amount;

        var rate = GetCurrentRate(targetCurrencyCode)
            ?? throw new DomainException($"No exchange rate found for {Code} to {targetCurrencyCode}");

        var convertedAmount = amount.Amount * rate.Rate;
        return new Money(Round(convertedAmount, targetCurrencyCode), targetCurrencyCode);
    }

    #endregion

    #region Configuration

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void ClearPrimary()
    {
        IsPrimary = false;
    }

    public void SetFormatting(
        string decimalSeparator,
        string thousandsSeparator,
        bool symbolFirst)
    {
        DecimalSeparator = decimalSeparator;
        ThousandsSeparator = thousandsSeparator;
        SymbolFirst = symbolFirst;
    }

    public void SetRoundingRule(RoundingRule rule)
    {
        RoundingRule = rule;
    }

    public void SetLimits(decimal? minimum, decimal? maximum)
    {
        if (minimum.HasValue && maximum.HasValue && minimum > maximum)
            throw new DomainException("Minimum cannot be greater than maximum");

        MinimumAmount = minimum;
        MaximumAmount = maximum;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    #endregion

    #region Formatting

    public string Format(decimal amount)
    {
        var rounded = Round(amount);
        var formatted = rounded.ToString($"N{DecimalPlaces}")
            .Replace(",", "TEMP")
            .Replace(".", DecimalSeparator)
            .Replace("TEMP", ThousandsSeparator);

        return SymbolFirst 
            ? $"{Symbol}{formatted}" 
            : $"{formatted} {Symbol}";
    }

    public decimal Round(decimal amount, string? targetCurrency = null)
    {
        var decimals = DecimalPlaces;
        
        return RoundingRule switch
        {
            RoundingRule.Standard => Math.Round(amount, decimals, MidpointRounding.AwayFromZero),
            RoundingRule.RoundUp => Math.Ceiling(amount * (decimal)Math.Pow(10, decimals)) / (decimal)Math.Pow(10, decimals),
            RoundingRule.RoundDown => Math.Floor(amount * (decimal)Math.Pow(10, decimals)) / (decimal)Math.Pow(10, decimals),
            RoundingRule.Bankers => Math.Round(amount, decimals, MidpointRounding.ToEven),
            RoundingRule.NoDecimal => Math.Round(amount, 0),
            RoundingRule.Quarter => Math.Round(amount * 4) / 4, // Round to nearest 0.25
            RoundingRule.Nickel => Math.Round(amount * 20) / 20, // Round to nearest 0.05
            _ => Math.Round(amount, decimals)
        };
    }

    public bool ValidateAmount(decimal amount)
    {
        if (MinimumAmount.HasValue && amount < MinimumAmount.Value)
            return false;
        if (MaximumAmount.HasValue && amount > MaximumAmount.Value)
            return false;
        return true;
    }

    #endregion
}

public class ExchangeRate : Entity<Guid>
{
    public string TargetCurrencyCode { get; private set; }
    public decimal Rate { get; private set; }
    public decimal InverseRate => 1 / Rate;
    public ExchangeRateSource Source { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveUntil { get; private set; }

    internal ExchangeRate(Guid id, string targetCurrencyCode, decimal rate, ExchangeRateSource source)
    {
        Id = id;
        TargetCurrencyCode = targetCurrencyCode;
        Rate = rate;
        Source = source;
        IsActive = true;
        EffectiveFrom = DateTime.UtcNow;
    }

    private ExchangeRate() { }

    internal void Expire()
    {
        IsActive = false;
        EffectiveUntil = DateTime.UtcNow;
    }

    internal void UpdateRate(decimal newRate)
    {
        if (newRate <= 0)
            throw new DomainException("Exchange rate must be greater than zero");
        Rate = newRate;
    }
}

public enum RoundingRule
{
    Standard = 1,      // Standard rounding (0.5 rounds up)
    RoundUp = 2,       // Always round up
    RoundDown = 3,     // Always round down (truncate)
    Bankers = 4,       // Banker's rounding (0.5 rounds to even)
    NoDecimal = 5,     // Round to whole number
    Quarter = 6,       // Round to nearest 0.25
    Nickel = 7         // Round to nearest 0.05
}

public enum ExchangeRateSource
{
    Manual = 1,
    CentralBank = 2,
    Xe = 3,
    OpenExchangeRates = 4,
    Bloomberg = 5,
    Reuters = 6
}
