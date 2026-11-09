using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.TaxAggregate;

/// <summary>
/// Tax Configuration Aggregate - Tax calculation rules and rates
/// 
/// BUSINESS RULES:
/// - Tax rates vary by region/country
/// - Product categories may have different tax rates
/// - Tax exemptions for certain products/customers
/// - VAT/GST included or excluded pricing
/// - Tax-free thresholds supported
/// - Digital goods special handling
/// </summary>
public class TaxConfiguration : AggregateRoot<Guid>
{
    public string CountryCode { get; private set; } = string.Empty;
    public string? RegionCode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TaxType TaxType { get; private set; }
    public decimal DefaultRate { get; private set; }
    public bool IsTaxIncludedInPrice { get; private set; }
    public bool IsActive { get; private set; }
    public decimal? TaxFreeThreshold { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveUntil { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }

    private readonly List<TaxCategoryRate> _categoryRates = new();
    public IReadOnlyCollection<TaxCategoryRate> CategoryRates => _categoryRates.AsReadOnly();

    private readonly List<TaxExemption> _exemptions = new();
    public IReadOnlyCollection<TaxExemption> Exemptions => _exemptions.AsReadOnly();

    private readonly List<TaxRule> _rules = new();
    public IReadOnlyCollection<TaxRule> Rules => _rules.AsReadOnly();

    private TaxConfiguration() { }

    public static TaxConfiguration CreateVAT(
        string countryCode,
        string name,
        decimal defaultRate,
        bool isTaxIncludedInPrice = true,
        string? regionCode = null)
    {
        return Create(countryCode, name, TaxType.VAT, defaultRate, isTaxIncludedInPrice, regionCode);
    }

    public static TaxConfiguration CreateGST(
        string countryCode,
        string name,
        decimal defaultRate,
        bool isTaxIncludedInPrice = false,
        string? regionCode = null)
    {
        return Create(countryCode, name, TaxType.GST, defaultRate, isTaxIncludedInPrice, regionCode);
    }

    public static TaxConfiguration CreateSalesTax(
        string countryCode,
        string regionCode,
        string name,
        decimal defaultRate)
    {
        return Create(countryCode, name, TaxType.SalesTax, defaultRate, false, regionCode);
    }

    private static TaxConfiguration Create(
        string countryCode,
        string name,
        TaxType taxType,
        decimal defaultRate,
        bool isTaxIncludedInPrice,
        string? regionCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new DomainException("Country code is required");
        if (defaultRate < 0 || defaultRate > 100)
            throw new DomainException("Tax rate must be between 0% and 100%");

        return new TaxConfiguration
        {
            Id = Guid.NewGuid(),
            CountryCode = countryCode.ToUpperInvariant(),
            RegionCode = regionCode?.ToUpperInvariant(),
            Name = name,
            TaxType = taxType,
            DefaultRate = defaultRate,
            IsTaxIncludedInPrice = isTaxIncludedInPrice,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Category Rates

    public TaxCategoryRate AddCategoryRate(
        Guid categoryId,
        string categoryName,
        decimal rate,
        string? description = null)
    {
        if (_categoryRates.Any(c => c.CategoryId == categoryId))
            throw new DomainException("Category rate already exists");

        if (rate < 0 || rate > 100)
            throw new DomainException("Tax rate must be between 0% and 100%");

        var categoryRate = new TaxCategoryRate(Guid.NewGuid(), categoryId, categoryName, rate, description);
        _categoryRates.Add(categoryRate);
        UpdateModified();
        return categoryRate;
    }

    public void UpdateCategoryRate(Guid categoryId, decimal newRate)
    {
        var categoryRate = _categoryRates.FirstOrDefault(c => c.CategoryId == categoryId)
            ?? throw new DomainException("Category rate not found");

        categoryRate.UpdateRate(newRate);
        UpdateModified();
    }

    public void RemoveCategoryRate(Guid categoryId)
    {
        var categoryRate = _categoryRates.FirstOrDefault(c => c.CategoryId == categoryId)
            ?? throw new DomainException("Category rate not found");

        _categoryRates.Remove(categoryRate);
        UpdateModified();
    }

    #endregion

    #region Exemptions

    public TaxExemption AddExemption(
        ExemptionType type,
        string? entityId,
        string description,
        DateTime? expiresAt = null)
    {
        var exemption = new TaxExemption(
            Guid.NewGuid(),
            type,
            entityId,
            description,
            expiresAt);

        _exemptions.Add(exemption);
        UpdateModified();
        return exemption;
    }

    public void RemoveExemption(Guid exemptionId)
    {
        var exemption = _exemptions.FirstOrDefault(e => e.Id == exemptionId)
            ?? throw new DomainException("Exemption not found");

        _exemptions.Remove(exemption);
        UpdateModified();
    }

    #endregion

    #region Tax Rules

    public TaxRule AddRule(
        string name,
        TaxRuleType ruleType,
        string condition,
        decimal? rateOverride = null,
        bool exempt = false)
    {
        var rule = new TaxRule(
            Guid.NewGuid(),
            name,
            ruleType,
            condition,
            rateOverride,
            exempt,
            _rules.Count + 1);

        _rules.Add(rule);
        UpdateModified();
        return rule;
    }

    public void UpdateRulePriority(Guid ruleId, int newPriority)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId)
            ?? throw new DomainException("Rule not found");

        rule.UpdatePriority(newPriority);
        UpdateModified();
    }

    public void RemoveRule(Guid ruleId)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId)
            ?? throw new DomainException("Rule not found");

        _rules.Remove(rule);
        UpdateModified();
    }

    #endregion

    #region Tax Calculation

    public TaxCalculationResult CalculateTax(
        Money amount,
        Guid? categoryId,
        Guid? customerId,
        bool isDigitalGood = false)
    {
        // Check for exemptions
        if (customerId.HasValue && IsCustomerExempt(customerId.Value))
        {
            return TaxCalculationResult.Exempt(amount, "Customer is tax exempt");
        }

        // Check tax-free threshold
        if (TaxFreeThreshold.HasValue && amount.Amount < TaxFreeThreshold.Value)
        {
            return TaxCalculationResult.BelowThreshold(amount, TaxFreeThreshold.Value);
        }

        // Determine applicable rate
        var rate = DetermineApplicableRate(categoryId, isDigitalGood);

        // Calculate tax
        Money taxAmount;
        Money netAmount;

        if (IsTaxIncludedInPrice)
        {
            // Tax is included, extract it
            taxAmount = amount.Multiply(rate / (100 + rate));
            netAmount = amount.Subtract(taxAmount);
        }
        else
        {
            // Tax is additional
            taxAmount = amount.Multiply(rate / 100);
            netAmount = amount;
        }

        return new TaxCalculationResult(
            netAmount,
            taxAmount,
            amount.Add(IsTaxIncludedInPrice ? Money.Zero(amount.Currency) : taxAmount),
            rate,
            TaxType,
            Name,
            null);
    }

    private decimal DetermineApplicableRate(Guid? categoryId, bool isDigitalGood)
    {
        // Check category-specific rate
        if (categoryId.HasValue)
        {
            var categoryRate = _categoryRates.FirstOrDefault(c => c.CategoryId == categoryId.Value);
            if (categoryRate != null)
                return categoryRate.Rate;
        }

        // Check rules
        foreach (var rule in _rules.OrderBy(r => r.Priority))
        {
            if (rule.IsExempt)
                return 0;
            if (rule.RateOverride.HasValue)
                return rule.RateOverride.Value;
        }

        return DefaultRate;
    }

    private bool IsCustomerExempt(Guid customerId)
    {
        return _exemptions.Any(e => 
            e.Type == ExemptionType.Customer && 
            e.EntityId == customerId.ToString() &&
            !e.IsExpired);
    }

    public bool IsCategoryExempt(Guid categoryId)
    {
        return _exemptions.Any(e => 
            e.Type == ExemptionType.ProductCategory && 
            e.EntityId == categoryId.ToString() &&
            !e.IsExpired);
    }

    #endregion

    #region Configuration Updates

    public void UpdateDefaultRate(decimal newRate)
    {
        if (newRate < 0 || newRate > 100)
            throw new DomainException("Tax rate must be between 0% and 100%");

        DefaultRate = newRate;
        UpdateModified();
    }

    public void SetTaxFreeThreshold(decimal? threshold)
    {
        TaxFreeThreshold = threshold;
        UpdateModified();
    }

    public void SetEffectiveDates(DateTime from, DateTime? until)
    {
        if (until.HasValue && until.Value <= from)
            throw new DomainException("End date must be after start date");

        EffectiveFrom = from;
        EffectiveUntil = until;
        UpdateModified();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private void UpdateModified() => LastModifiedAt = DateTime.UtcNow;

    #endregion
}

public class TaxCategoryRate : Entity<Guid>
{
    public Guid CategoryId { get; private set; }
    public string CategoryName { get; private set; }
    public decimal Rate { get; private set; }
    public string? Description { get; private set; }

    internal TaxCategoryRate(Guid id, Guid categoryId, string categoryName, decimal rate, string? description)
    {
        Id = id;
        CategoryId = categoryId;
        CategoryName = categoryName;
        Rate = rate;
        Description = description;
    }

    private TaxCategoryRate() { }

    internal void UpdateRate(decimal rate)
    {
        if (rate < 0 || rate > 100)
            throw new DomainException("Tax rate must be between 0% and 100%");
        Rate = rate;
    }
}

public class TaxExemption : Entity<Guid>
{
    public ExemptionType Type { get; private set; }
    public string? EntityId { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    internal TaxExemption(Guid id, ExemptionType type, string? entityId, string description, DateTime? expiresAt)
    {
        Id = id;
        Type = type;
        EntityId = entityId;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    private TaxExemption() { }
}

public class TaxRule : Entity<Guid>
{
    public string Name { get; private set; }
    public TaxRuleType RuleType { get; private set; }
    public string Condition { get; private set; }
    public decimal? RateOverride { get; private set; }
    public bool IsExempt { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    internal TaxRule(Guid id, string name, TaxRuleType ruleType, string condition, decimal? rateOverride, bool isExempt, int priority)
    {
        Id = id;
        Name = name;
        RuleType = ruleType;
        Condition = condition;
        RateOverride = rateOverride;
        IsExempt = isExempt;
        Priority = priority;
        IsActive = true;
    }

    private TaxRule() { }

    internal void UpdatePriority(int priority) => Priority = priority;
}

public class TaxCalculationResult
{
    public Money NetAmount { get; }
    public Money TaxAmount { get; }
    public Money GrossAmount { get; }
    public decimal AppliedRate { get; }
    public TaxType TaxType { get; }
    public string TaxName { get; }
    public string? ExemptionReason { get; }
    public bool IsExempt => ExemptionReason != null;

    public TaxCalculationResult(
        Money netAmount,
        Money taxAmount,
        Money grossAmount,
        decimal appliedRate,
        TaxType taxType,
        string taxName,
        string? exemptionReason)
    {
        NetAmount = netAmount;
        TaxAmount = taxAmount;
        GrossAmount = grossAmount;
        AppliedRate = appliedRate;
        TaxType = taxType;
        TaxName = taxName;
        ExemptionReason = exemptionReason;
    }

    public static TaxCalculationResult Exempt(Money amount, string reason)
    {
        return new TaxCalculationResult(
            amount,
            Money.Zero(amount.Currency),
            amount,
            0,
            TaxType.None,
            "Exempt",
            reason);
    }

    public static TaxCalculationResult BelowThreshold(Money amount, decimal threshold)
    {
        return new TaxCalculationResult(
            amount,
            Money.Zero(amount.Currency),
            amount,
            0,
            TaxType.None,
            "Below Threshold",
            $"Order below tax-free threshold of {threshold}");
    }
}

public enum TaxType
{
    None = 0,
    VAT = 1,           // Value Added Tax (EU, UK, etc.)
    GST = 2,           // Goods and Services Tax (AUS, NZ, India, etc.)
    SalesTax = 3,      // US-style sales tax
    ServiceTax = 4,
    ImportDuty = 5,
    ExciseTax = 6
}

public enum ExemptionType
{
    Customer = 1,           // Specific customer exempt
    ProductCategory = 2,    // Entire category exempt
    Product = 3,            // Specific product exempt
    OrderType = 4,          // B2B orders, etc.
    Region = 5              // Specific region exempt
}

public enum TaxRuleType
{
    DigitalGoods = 1,       // Special handling for digital products
    CrossBorder = 2,        // International sales
    B2B = 3,                // Business-to-business
    Essential = 4,          // Essential goods (food, medicine)
    Luxury = 5,             // Luxury items
    Reseller = 6            // Reseller/wholesale
}
