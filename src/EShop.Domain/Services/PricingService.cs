using EShop.Domain.Aggregates.CustomerAggregate;
using EShop.Domain.Aggregates.PromotionAggregate;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Services;

/// <summary>
/// Domain Service - Pricing calculations involving multiple aggregates
/// 
/// This handles complex pricing logic that doesn't belong to a single aggregate:
/// - Dynamic pricing based on customer tier
/// - Promotional discounts
/// - Tax calculations
/// - Shipping cost calculations
/// </summary>
public interface IPricingService
{
    PricingResult CalculateOrderPricing(
        IEnumerable<PricingLineItem> items,
        CustomerProfile? customer,
        Promotion? promotion,
        string shippingCountry,
        string currency = "KWD");

    Money CalculateTax(Money subtotal, string country, string currency);
    Money CalculateTierDiscount(Money subtotal, CustomerTier tier, string currency);
    decimal GetTierDiscountPercentage(CustomerTier tier);
}

public class PricingService : IPricingService
{
    private readonly TaxConfiguration _taxConfig;

    public PricingService(TaxConfiguration taxConfig)
    {
        _taxConfig = taxConfig;
    }

    public PricingResult CalculateOrderPricing(
        IEnumerable<PricingLineItem> items,
        CustomerProfile? customer,
        Promotion? promotion,
        string shippingCountry,
        string currency = "KWD")
    {
        var itemsList = items.ToList();
        var result = new PricingResult(currency);

        // 1. Calculate subtotal from all items
        foreach (var item in itemsList)
        {
            var lineTotal = item.UnitPrice.Multiply(item.Quantity);
            result.AddLineItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, lineTotal);
        }

        var subtotal = result.Subtotal;

        // 2. Apply customer tier discount
        if (customer != null)
        {
            var tierDiscount = CalculateTierDiscount(subtotal, customer.Tier, currency);
            if (tierDiscount.Amount > 0)
            {
                result.ApplyTierDiscount(tierDiscount, customer.Tier);
            }
        }

        // 3. Apply promotional discount
        if (promotion != null)
        {
            var eligibleAmount = GetPromotionEligibleAmount(itemsList, promotion);
            var promoDiscount = promotion.CalculateDiscount(eligibleAmount, currency);
            
            if (promoDiscount.Amount > 0)
            {
                result.ApplyPromotionalDiscount(promoDiscount, promotion.Code);
            }
        }

        // 4. Calculate tax
        var taxableAmount = result.SubtotalAfterDiscounts;
        var tax = CalculateTax(taxableAmount, shippingCountry, currency);
        result.SetTax(tax, _taxConfig.GetTaxRate(shippingCountry));

        // 5. Calculate shipping
        var shippingCost = CalculateShipping(result.SubtotalAfterDiscounts, shippingCountry, currency);
        result.SetShipping(shippingCost);

        // 6. Apply free shipping threshold
        if (result.SubtotalAfterDiscounts.Amount >= _taxConfig.FreeShippingThreshold)
        {
            result.SetFreeShipping();
        }

        return result;
    }

    public Money CalculateTax(Money subtotal, string country, string currency)
    {
        var taxRate = _taxConfig.GetTaxRate(country);
        return subtotal.Multiply(taxRate);
    }

    public Money CalculateTierDiscount(Money subtotal, CustomerTier tier, string currency)
    {
        var discountPercentage = GetTierDiscountPercentage(tier);
        return subtotal.Multiply(discountPercentage);
    }

    public decimal GetTierDiscountPercentage(CustomerTier tier)
    {
        return tier switch
        {
            CustomerTier.Bronze => 0m,
            CustomerTier.Silver => 0.05m,  // 5%
            CustomerTier.Gold => 0.10m,    // 10%
            CustomerTier.Platinum => 0.15m, // 15%
            _ => 0m
        };
    }

    private decimal GetPromotionEligibleAmount(
        List<PricingLineItem> items, 
        Promotion promotion)
    {
        if (promotion.Scope == PromotionScope.StoreWide)
        {
            return items.Sum(i => i.UnitPrice.Amount * i.Quantity);
        }

        if (promotion.Scope == PromotionScope.SpecificProducts)
        {
            return items
                .Where(i => promotion.ApplicableProductIds.Contains(i.ProductId))
                .Sum(i => i.UnitPrice.Amount * i.Quantity);
        }

        return items.Sum(i => i.UnitPrice.Amount * i.Quantity);
    }

    private Money CalculateShipping(Money orderValue, string country, string currency)
    {
        // Simple shipping calculation - can be extended with carrier integration
        var baseRate = _taxConfig.GetBaseShippingRate(country);
        return new Money(baseRate, currency);
    }
}

public class PricingResult
{
    private readonly string _currency;
    private readonly List<PricingLineResult> _lineItems = new();

    public IReadOnlyCollection<PricingLineResult> LineItems => _lineItems.AsReadOnly();
    public Money Subtotal { get; private set; }
    public Money TierDiscount { get; private set; }
    public CustomerTier? AppliedTier { get; private set; }
    public Money PromotionalDiscount { get; private set; }
    public string? AppliedPromoCode { get; private set; }
    public Money SubtotalAfterDiscounts => Subtotal.Subtract(TierDiscount).Subtract(PromotionalDiscount);
    public Money Tax { get; private set; }
    public decimal TaxRate { get; private set; }
    public Money Shipping { get; private set; }
    public bool IsFreeShipping { get; private set; }
    public Money Total => SubtotalAfterDiscounts.Add(Tax).Add(Shipping);
    public Money TotalSavings => TierDiscount.Add(PromotionalDiscount);

    public PricingResult(string currency)
    {
        _currency = currency;
        Subtotal = Money.Zero(currency);
        TierDiscount = Money.Zero(currency);
        PromotionalDiscount = Money.Zero(currency);
        Tax = Money.Zero(currency);
        Shipping = Money.Zero(currency);
    }

    public void AddLineItem(Guid productId, string productName, Money unitPrice, int quantity, Money lineTotal)
    {
        _lineItems.Add(new PricingLineResult(productId, productName, unitPrice, quantity, lineTotal));
        Subtotal = Subtotal.Add(lineTotal);
    }

    public void ApplyTierDiscount(Money discount, CustomerTier tier)
    {
        TierDiscount = discount;
        AppliedTier = tier;
    }

    public void ApplyPromotionalDiscount(Money discount, string promoCode)
    {
        PromotionalDiscount = discount;
        AppliedPromoCode = promoCode;
    }

    public void SetTax(Money tax, decimal taxRate)
    {
        Tax = tax;
        TaxRate = taxRate;
    }

    public void SetShipping(Money shipping)
    {
        Shipping = shipping;
    }

    public void SetFreeShipping()
    {
        IsFreeShipping = true;
        Shipping = Money.Zero(_currency);
    }
}

public class PricingLineResult
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public Money UnitPrice { get; }
    public int Quantity { get; }
    public Money LineTotal { get; }

    public PricingLineResult(Guid productId, string productName, Money unitPrice, int quantity, Money lineTotal)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = lineTotal;
    }
}

public class PricingLineItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public Money UnitPrice { get; }
    public int Quantity { get; }
    public Guid? CategoryId { get; }

    public PricingLineItem(Guid productId, string productName, Money unitPrice, int quantity, Guid? categoryId = null)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        CategoryId = categoryId;
    }
}

public class TaxConfiguration
{
    private readonly Dictionary<string, decimal> _taxRates = new()
    {
        ["KW"] = 0m,        // Kuwait - no VAT
        ["AE"] = 0.05m,     // UAE - 5% VAT
        ["SA"] = 0.15m,     // Saudi Arabia - 15% VAT
        ["US"] = 0m,        // Varies by state
        ["GB"] = 0.20m,     // UK - 20% VAT
        ["DE"] = 0.19m,     // Germany - 19% VAT
    };

    private readonly Dictionary<string, decimal> _shippingRates = new()
    {
        ["KW"] = 2.0m,
        ["AE"] = 5.0m,
        ["SA"] = 5.0m,
        ["US"] = 15.0m,
        ["GB"] = 12.0m,
        ["DE"] = 10.0m,
    };

    public decimal FreeShippingThreshold { get; set; } = 50m;

    public decimal GetTaxRate(string countryCode)
    {
        return _taxRates.TryGetValue(countryCode.ToUpperInvariant(), out var rate) ? rate : 0m;
    }

    public decimal GetBaseShippingRate(string countryCode)
    {
        return _shippingRates.TryGetValue(countryCode.ToUpperInvariant(), out var rate) ? rate : 10m;
    }
}
