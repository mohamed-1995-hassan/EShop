using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.BundleAggregate;

/// <summary>
/// Bundle/Kit Aggregate - Product bundling and kits
/// 
/// BUSINESS RULES:
/// - Bundle contains multiple products at discounted price
/// - Fixed bundles vs customizable bundles
/// - Inventory checked across all items
/// - Bundle discount can be percentage or fixed amount
/// - Items can be required or optional
/// - Maximum quantities per item
/// </summary>
public class Bundle : AggregateRoot<Guid>
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public BundleType Type { get; private set; }
    public BundleStatus Status { get; private set; }
    public Money BasePrice { get; private set; } = null!;
    public Money BundlePrice { get; private set; } = null!;
    public Money SavingsAmount { get; private set; } = null!;
    public decimal SavingsPercentage { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public int? MaxQuantityPerOrder { get; private set; }
    public int MinItemsRequired { get; private set; }
    public int MaxItemsAllowed { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<BundleItem> _items = new();
    public IReadOnlyCollection<BundleItem> Items => _items.AsReadOnly();

    private readonly List<BundleCategory> _categories = new();
    public IReadOnlyCollection<BundleCategory> Categories => _categories.AsReadOnly();

    public bool IsActive => Status == BundleStatus.Active && IsWithinValidityPeriod;
    public bool IsWithinValidityPeriod => 
        (!ValidFrom.HasValue || DateTime.UtcNow >= ValidFrom.Value) &&
        (!ValidUntil.HasValue || DateTime.UtcNow <= ValidUntil.Value);
    public int TotalItems => _items.Sum(i => i.Quantity);
    public int RequiredItemCount => _items.Count(i => i.IsRequired);

    private Bundle() { }

    public static Bundle CreateFixed(
        string sku,
        string name,
        string description,
        DiscountType discountType,
        decimal discountValue,
        string? imageUrl = null)
    {
        return Create(sku, name, description, BundleType.Fixed, discountType, discountValue, imageUrl);
    }

    public static Bundle CreateCustomizable(
        string sku,
        string name,
        string description,
        int minItems,
        int maxItems,
        DiscountType discountType,
        decimal discountValue,
        string? imageUrl = null)
    {
        var bundle = Create(sku, name, description, BundleType.Customizable, discountType, discountValue, imageUrl);
        bundle.MinItemsRequired = minItems;
        bundle.MaxItemsAllowed = maxItems;
        return bundle;
    }

    public static Bundle CreateMixAndMatch(
        string sku,
        string name,
        string description,
        int requiredCount,
        DiscountType discountType,
        decimal discountValue,
        string? imageUrl = null)
    {
        var bundle = Create(sku, name, description, BundleType.MixAndMatch, discountType, discountValue, imageUrl);
        bundle.MinItemsRequired = requiredCount;
        bundle.MaxItemsAllowed = requiredCount;
        return bundle;
    }

    private static Bundle Create(
        string sku,
        string name,
        string description,
        BundleType type,
        DiscountType discountType,
        decimal discountValue,
        string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Bundle SKU is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Bundle name is required");
        if (discountValue <= 0)
            throw new DomainException("Discount value must be positive");
        if (discountType == DiscountType.Percentage && discountValue > 100)
            throw new DomainException("Percentage discount cannot exceed 100%");

        return new Bundle
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            Type = type,
            Status = BundleStatus.Draft,
            BasePrice = Money.Zero("KWD"),
            BundlePrice = Money.Zero("KWD"),
            SavingsAmount = Money.Zero("KWD"),
            SavingsPercentage = 0,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MinItemsRequired = 0,
            MaxItemsAllowed = 100,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Item Management

    public BundleItem AddItem(
        Guid productId,
        string productName,
        string productSku,
        Money unitPrice,
        int quantity = 1,
        bool isRequired = true,
        int? minQuantity = null,
        int? maxQuantity = null)
    {
        if (_items.Any(i => i.ProductId == productId))
            throw new DomainException("Product already in bundle");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        var item = new BundleItem(
            Guid.NewGuid(),
            productId,
            productName,
            productSku,
            unitPrice,
            quantity,
            isRequired,
            minQuantity ?? (isRequired ? quantity : 0),
            maxQuantity ?? quantity,
            _items.Count + 1);

        _items.Add(item);
        RecalculatePricing();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in bundle");

        _items.Remove(item);
        ReorderItems();
        RecalculatePricing();
    }

    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in bundle");

        item.UpdateQuantity(quantity);
        RecalculatePricing();
    }

    public void UpdateItemPrice(Guid productId, Money newUnitPrice)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.UpdatePrice(newUnitPrice);
            RecalculatePricing();
        }
    }

    public void ReorderItem(Guid itemId, int newPosition)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found");

        if (newPosition < 1 || newPosition > _items.Count)
            throw new DomainException("Invalid position");

        _items.Remove(item);
        _items.Insert(newPosition - 1, item);
        ReorderItems();
    }

    private void ReorderItems()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].SetPosition(i + 1);
        }
    }

    #endregion

    #region Category Management (for Mix & Match)

    public BundleCategory AddCategory(
        Guid categoryId,
        string categoryName,
        int minSelections,
        int maxSelections)
    {
        if (Type != BundleType.MixAndMatch && Type != BundleType.Customizable)
            throw new DomainException("Categories only available for customizable bundles");

        var category = new BundleCategory(
            Guid.NewGuid(),
            categoryId,
            categoryName,
            minSelections,
            maxSelections);

        _categories.Add(category);
        return category;
    }

    public void AddItemToCategory(Guid categoryId, Guid itemId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new DomainException("Category not found");

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found");

        category.AddItem(itemId);
    }

    #endregion

    #region Pricing

    private void RecalculatePricing()
    {
        if (!_items.Any())
        {
            BasePrice = Money.Zero("KWD");
            BundlePrice = Money.Zero("KWD");
            SavingsAmount = Money.Zero("KWD");
            SavingsPercentage = 0;
            return;
        }

        var currency = _items.First().UnitPrice.Currency;
        var totalBasePrice = _items.Sum(i => i.UnitPrice.Amount * i.Quantity);
        BasePrice = new Money(totalBasePrice, currency);

        var bundlePrice = DiscountType switch
        {
            DiscountType.Percentage => totalBasePrice * (1 - DiscountValue / 100),
            DiscountType.FixedAmount => totalBasePrice - DiscountValue,
            DiscountType.FixedPrice => DiscountValue,
            _ => totalBasePrice
        };

        bundlePrice = Math.Max(bundlePrice, 0);
        BundlePrice = new Money(bundlePrice, currency);
        SavingsAmount = new Money(totalBasePrice - bundlePrice, currency);
        SavingsPercentage = totalBasePrice > 0 
            ? Math.Round((1 - bundlePrice / totalBasePrice) * 100, 1) 
            : 0;
    }

    public Money CalculatePrice(IReadOnlyDictionary<Guid, int>? selectedQuantities = null)
    {
        if (Type == BundleType.Fixed || selectedQuantities == null)
            return BundlePrice;

        // For customizable bundles, calculate based on selections
        var totalBasePrice = 0m;
        foreach (var item in _items)
        {
            var quantity = selectedQuantities.TryGetValue(item.ProductId, out var q) ? q : 0;
            if (item.IsRequired && quantity < item.MinQuantity)
                quantity = item.MinQuantity;
            quantity = Math.Min(quantity, item.MaxQuantity);
            totalBasePrice += item.UnitPrice.Amount * quantity;
        }

        var bundlePrice = DiscountType switch
        {
            DiscountType.Percentage => totalBasePrice * (1 - DiscountValue / 100),
            DiscountType.FixedAmount => totalBasePrice - DiscountValue,
            DiscountType.FixedPrice => DiscountValue,
            _ => totalBasePrice
        };

        return new Money(Math.Max(bundlePrice, 0), BundlePrice.Currency);
    }

    #endregion

    #region Validation

    public BundleValidationResult ValidateSelection(IReadOnlyDictionary<Guid, int> selectedQuantities)
    {
        var errors = new List<string>();

        // Check required items
        foreach (var item in _items.Where(i => i.IsRequired))
        {
            if (!selectedQuantities.TryGetValue(item.ProductId, out var qty) || qty < item.MinQuantity)
            {
                errors.Add($"Required item '{item.ProductName}' must have at least {item.MinQuantity} quantity");
            }
        }

        // Check min/max quantities per item
        foreach (var (productId, quantity) in selectedQuantities)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
            {
                errors.Add($"Product {productId} is not part of this bundle");
                continue;
            }
            if (quantity < item.MinQuantity)
                errors.Add($"'{item.ProductName}' requires minimum {item.MinQuantity} quantity");
            if (quantity > item.MaxQuantity)
                errors.Add($"'{item.ProductName}' allows maximum {item.MaxQuantity} quantity");
        }

        // Check total items for customizable bundles
        var totalItems = selectedQuantities.Sum(kv => kv.Value);
        if (Type == BundleType.Customizable || Type == BundleType.MixAndMatch)
        {
            if (totalItems < MinItemsRequired)
                errors.Add($"Bundle requires at least {MinItemsRequired} items");
            if (totalItems > MaxItemsAllowed)
                errors.Add($"Bundle allows maximum {MaxItemsAllowed} items");
        }

        // Check category constraints
        foreach (var category in _categories)
        {
            var categoryItems = _items.Where(i => category.ItemIds.Contains(i.Id));
            var categoryTotal = categoryItems.Sum(i => 
                selectedQuantities.TryGetValue(i.ProductId, out var q) ? q : 0);

            if (categoryTotal < category.MinSelections)
                errors.Add($"Category '{category.Name}' requires at least {category.MinSelections} selections");
            if (categoryTotal > category.MaxSelections)
                errors.Add($"Category '{category.Name}' allows maximum {category.MaxSelections} selections");
        }

        return new BundleValidationResult(errors.Count == 0, errors);
    }

    #endregion

    #region Lifecycle

    public void Activate()
    {
        if (!_items.Any())
            throw new DomainException("Bundle must have at least one item");

        if (Type == BundleType.Fixed && _items.Any(i => !i.IsRequired))
            throw new DomainException("Fixed bundles cannot have optional items");

        Status = BundleStatus.Active;
    }

    public void Deactivate() => Status = BundleStatus.Inactive;
    public void Archive() => Status = BundleStatus.Archived;

    public void SetValidityPeriod(DateTime? from, DateTime? until)
    {
        if (from.HasValue && until.HasValue && from.Value >= until.Value)
            throw new DomainException("End date must be after start date");

        ValidFrom = from;
        ValidUntil = until;
    }

    public void SetMaxQuantityPerOrder(int? maxQuantity)
    {
        if (maxQuantity.HasValue && maxQuantity.Value <= 0)
            throw new DomainException("Max quantity must be positive");

        MaxQuantityPerOrder = maxQuantity;
    }

    public void UpdateDetails(string name, string description, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");

        Name = name;
        Description = description;
        ImageUrl = imageUrl;
    }

    public void UpdateDiscount(DiscountType type, decimal value)
    {
        if (value <= 0)
            throw new DomainException("Discount value must be positive");
        if (type == DiscountType.Percentage && value > 100)
            throw new DomainException("Percentage cannot exceed 100%");

        DiscountType = type;
        DiscountValue = value;
        RecalculatePricing();
    }

    #endregion
}

public class BundleItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public string ProductSku { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public bool IsRequired { get; private set; }
    public int MinQuantity { get; private set; }
    public int MaxQuantity { get; private set; }
    public int Position { get; private set; }

    public Money TotalPrice => UnitPrice.Multiply(Quantity);

    internal BundleItem(
        Guid id,
        Guid productId,
        string productName,
        string productSku,
        Money unitPrice,
        int quantity,
        bool isRequired,
        int minQuantity,
        int maxQuantity,
        int position)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        ProductSku = productSku;
        UnitPrice = unitPrice;
        Quantity = quantity;
        IsRequired = isRequired;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        Position = position;
    }

    private BundleItem() { }

    internal void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
        Quantity = quantity;
    }

    internal void UpdatePrice(Money newPrice) => UnitPrice = newPrice;
    internal void SetPosition(int position) => Position = position;
}

public class BundleCategory : Entity<Guid>
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public int MinSelections { get; private set; }
    public int MaxSelections { get; private set; }

    private readonly List<Guid> _itemIds = new();
    public IReadOnlyCollection<Guid> ItemIds => _itemIds.AsReadOnly();

    internal BundleCategory(Guid id, Guid categoryId, string name, int minSelections, int maxSelections)
    {
        Id = id;
        CategoryId = categoryId;
        Name = name;
        MinSelections = minSelections;
        MaxSelections = maxSelections;
    }

    private BundleCategory() { }

    internal void AddItem(Guid itemId)
    {
        if (!_itemIds.Contains(itemId))
            _itemIds.Add(itemId);
    }

    internal void RemoveItem(Guid itemId) => _itemIds.Remove(itemId);
}

public record BundleValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public enum BundleType
{
    Fixed = 1,          // All items required, no customization
    Customizable = 2,   // Can add/remove items within rules
    MixAndMatch = 3,    // Pick X items from categories
    BOGO = 4,           // Buy one get one
    Tiered = 5          // Discount increases with quantity
}

public enum BundleStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Archived = 4
}

public enum DiscountType
{
    Percentage = 1,     // X% off total
    FixedAmount = 2,    // $X off total
    FixedPrice = 3      // Bundle costs exactly $X
}
