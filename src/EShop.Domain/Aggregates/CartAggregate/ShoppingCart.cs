using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.CartAggregate;

/// <summary>
/// Shopping Cart Aggregate - Manages customer's shopping session
/// 
/// BUSINESS RULES:
/// - Cart expires after 30 days of inactivity
/// - Maximum 50 items per cart
/// - Price locks for 15 minutes during checkout
/// - Promotional codes validated at checkout
/// - Saved for later items managed separately
/// </summary>
public class ShoppingCart : AggregateRoot<Guid>
{
    public Guid? CustomerId { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public CartStatus Status { get; private set; }
    public Money Subtotal { get; private set; } = null!;
    public Money Discount { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money Tax { get; private set; } = null!;
    public Money Total { get; private set; } = null!;
    public string Currency { get; private set; } = "KWD";
    public string? AppliedPromoCode { get; private set; }
    public Guid? AppliedPromotionId { get; private set; }
    public int? LoyaltyPointsToRedeem { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public DateTime? CheckoutStartedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private readonly List<SavedForLaterItem> _savedItems = new();
    public IReadOnlyCollection<SavedForLaterItem> SavedItems => _savedItems.AsReadOnly();

    public int ItemCount => _items.Sum(i => i.Quantity);
    public bool IsEmpty => !_items.Any();
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool IsInCheckout => Status == CartStatus.CheckingOut && 
                                CheckoutStartedAt.HasValue &&
                                DateTime.UtcNow <= CheckoutStartedAt.Value.AddMinutes(15);

    private ShoppingCart() { }

    public static ShoppingCart CreateForGuest(string sessionId, string currency = "KWD")
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new DomainException("Session ID is required");

        return CreateCart(null, sessionId, currency);
    }

    public static ShoppingCart CreateForCustomer(Guid customerId, string sessionId, string currency = "KWD")
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required");

        return CreateCart(customerId, sessionId, currency);
    }

    private static ShoppingCart CreateCart(Guid? customerId, string sessionId, string currency)
    {
        return new ShoppingCart
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SessionId = sessionId,
            Status = CartStatus.Active,
            Currency = currency,
            Subtotal = Money.Zero(currency),
            Discount = Money.Zero(currency),
            ShippingCost = Money.Zero(currency),
            Tax = Money.Zero(currency),
            Total = Money.Zero(currency),
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
    }

    #region Item Management

    public CartItem AddItem(
        Guid productId,
        string productName,
        string? sku,
        Money unitPrice,
        int quantity,
        string? imageUrl = null,
        Dictionary<string, string>? attributes = null)
    {
        ValidateCanModify();

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        if (_items.Count >= 50 && !_items.Any(i => i.ProductId == productId))
            throw new DomainException("Cart cannot contain more than 50 different items");

        var existingItem = _items.FirstOrDefault(i => 
            i.ProductId == productId && 
            AttributesMatch(i.Attributes, attributes));

        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            UpdateActivity();
            RecalculateTotals();
            return existingItem;
        }

        var item = new CartItem(
            Guid.NewGuid(),
            productId,
            productName,
            sku,
            unitPrice,
            quantity,
            imageUrl,
            attributes);

        _items.Add(item);
        UpdateActivity();
        RecalculateTotals();
        return item;
    }

    public void UpdateItemQuantity(Guid itemId, int newQuantity)
    {
        ValidateCanModify();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in cart");

        if (newQuantity <= 0)
        {
            RemoveItem(itemId);
            return;
        }

        item.UpdateQuantity(newQuantity);
        UpdateActivity();
        RecalculateTotals();
    }

    public void RemoveItem(Guid itemId)
    {
        ValidateCanModify();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in cart");

        _items.Remove(item);
        UpdateActivity();
        RecalculateTotals();
    }

    public void Clear()
    {
        ValidateCanModify();
        _items.Clear();
        ClearPromotion();
        UpdateActivity();
        RecalculateTotals();
    }

    #endregion

    #region Save for Later

    public void SaveItemForLater(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in cart");

        _savedItems.Add(new SavedForLaterItem(
            Guid.NewGuid(),
            item.ProductId,
            item.ProductName,
            item.UnitPrice,
            item.ImageUrl));

        _items.Remove(item);
        UpdateActivity();
        RecalculateTotals();
    }

    public void MoveToCart(Guid savedItemId, int quantity = 1)
    {
        var savedItem = _savedItems.FirstOrDefault(i => i.Id == savedItemId)
            ?? throw new DomainException("Saved item not found");

        AddItem(
            savedItem.ProductId,
            savedItem.ProductName,
            null,
            savedItem.UnitPrice,
            quantity,
            savedItem.ImageUrl);

        _savedItems.Remove(savedItem);
    }

    public void RemoveSavedItem(Guid savedItemId)
    {
        var item = _savedItems.FirstOrDefault(i => i.Id == savedItemId)
            ?? throw new DomainException("Saved item not found");

        _savedItems.Remove(item);
    }

    #endregion

    #region Promotions & Loyalty

    public void ApplyPromoCode(Guid promotionId, string promoCode, Money discount)
    {
        ValidateCanModify();

        AppliedPromotionId = promotionId;
        AppliedPromoCode = promoCode;
        Discount = discount;
        RecalculateTotals();
        UpdateActivity();
    }

    public void ClearPromotion()
    {
        AppliedPromotionId = null;
        AppliedPromoCode = null;
        Discount = Money.Zero(Currency);
        RecalculateTotals();
    }

    public void SetLoyaltyPointsRedemption(int points, Money value)
    {
        ValidateCanModify();

        if (points <= 0)
            throw new DomainException("Points must be greater than zero");

        LoyaltyPointsToRedeem = points;
        // Add points value to discount
        Discount = Discount.Add(value);
        RecalculateTotals();
        UpdateActivity();
    }

    public void ClearLoyaltyRedemption()
    {
        if (LoyaltyPointsToRedeem.HasValue)
        {
            LoyaltyPointsToRedeem = null;
            RecalculateTotals();
        }
    }

    #endregion

    #region Shipping & Tax

    public void SetShippingCost(Money shippingCost)
    {
        ShippingCost = shippingCost;
        RecalculateTotals();
    }

    public void SetTax(Money tax)
    {
        Tax = tax;
        RecalculateTotals();
    }

    public void SetFreeShipping()
    {
        ShippingCost = Money.Zero(Currency);
        RecalculateTotals();
    }

    #endregion

    #region Checkout Flow

    public void StartCheckout()
    {
        if (IsEmpty)
            throw new DomainException("Cannot checkout empty cart");

        if (Status == CartStatus.Converted)
            throw new DomainException("Cart already converted to order");

        Status = CartStatus.CheckingOut;
        CheckoutStartedAt = DateTime.UtcNow;
        UpdateActivity();
    }

    public void AbandonCheckout()
    {
        if (Status != CartStatus.CheckingOut)
            return;

        Status = CartStatus.Abandoned;
        UpdateActivity();
    }

    public void ConvertToOrder(Guid orderId)
    {
        if (Status != CartStatus.CheckingOut)
            throw new DomainException("Cart must be in checkout to convert");

        if (IsEmpty)
            throw new DomainException("Cannot convert empty cart");

        Status = CartStatus.Converted;
        UpdateActivity();
    }

    public void ReactivateAbandonedCart()
    {
        if (Status != CartStatus.Abandoned)
            throw new DomainException("Only abandoned carts can be reactivated");

        Status = CartStatus.Active;
        ExpiresAt = DateTime.UtcNow.AddDays(30);
        UpdateActivity();
    }

    #endregion

    #region Merge & Transfer

    public void AssignToCustomer(Guid customerId)
    {
        if (CustomerId.HasValue && CustomerId != customerId)
            throw new DomainException("Cart already assigned to different customer");

        CustomerId = customerId;
        UpdateActivity();
    }

    public void MergeWith(ShoppingCart otherCart)
    {
        if (otherCart.Id == Id)
            throw new DomainException("Cannot merge cart with itself");

        foreach (var item in otherCart.Items)
        {
            var existingItem = _items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
            {
                existingItem.UpdateQuantity(existingItem.Quantity + item.Quantity);
            }
            else
            {
                _items.Add(new CartItem(
                    Guid.NewGuid(),
                    item.ProductId,
                    item.ProductName,
                    item.Sku,
                    item.UnitPrice,
                    item.Quantity,
                    item.ImageUrl,
                    item.Attributes?.ToDictionary(a => a.Key, a => a.Value)));
            }
        }

        foreach (var savedItem in otherCart.SavedItems)
        {
            if (!_savedItems.Any(s => s.ProductId == savedItem.ProductId))
            {
                _savedItems.Add(new SavedForLaterItem(
                    Guid.NewGuid(),
                    savedItem.ProductId,
                    savedItem.ProductName,
                    savedItem.UnitPrice,
                    savedItem.ImageUrl));
            }
        }

        RecalculateTotals();
        UpdateActivity();
    }

    #endregion

    #region Price Updates

    public void UpdateItemPrice(Guid productId, Money newPrice)
    {
        var items = _items.Where(i => i.ProductId == productId).ToList();
        foreach (var item in items)
        {
            item.UpdatePrice(newPrice);
        }
        RecalculateTotals();
    }

    public void RefreshPrices(Dictionary<Guid, Money> currentPrices)
    {
        foreach (var item in _items)
        {
            if (currentPrices.TryGetValue(item.ProductId, out var currentPrice))
            {
                item.UpdatePrice(currentPrice);
            }
        }
        RecalculateTotals();
    }

    #endregion

    #region Private Methods

    private void ValidateCanModify()
    {
        if (Status == CartStatus.Converted)
            throw new DomainException("Cannot modify converted cart");

        if (IsExpired)
            throw new DomainException("Cart has expired");
    }

    private void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(30);
    }

    private void RecalculateTotals()
    {
        Subtotal = _items.Aggregate(
            Money.Zero(Currency),
            (total, item) => total.Add(item.LineTotal));

        Total = Subtotal
            .Subtract(Discount)
            .Add(ShippingCost)
            .Add(Tax);

        // Ensure total doesn't go negative
        if (Total.Amount < 0)
            Total = Money.Zero(Currency);
    }

    private static bool AttributesMatch(
        IReadOnlyDictionary<string, string>? attrs1,
        Dictionary<string, string>? attrs2)
    {
        if (attrs1 == null && attrs2 == null) return true;
        if (attrs1 == null || attrs2 == null) return false;
        if (attrs1.Count != attrs2.Count) return false;

        return attrs1.All(kvp => attrs2.TryGetValue(kvp.Key, out var value) && value == kvp.Value);
    }

    #endregion
}

public class CartItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public string? Sku { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string? ImageUrl { get; private set; }
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private readonly Dictionary<string, string> _attributes = new();
    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    internal CartItem(
        Guid id,
        Guid productId,
        string productName,
        string? sku,
        Money unitPrice,
        int quantity,
        string? imageUrl,
        Dictionary<string, string>? attributes)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        Quantity = quantity;
        ImageUrl = imageUrl;
        if (attributes != null)
        {
            foreach (var attr in attributes)
                _attributes[attr.Key] = attr.Value;
        }
    }

    private CartItem() { } // EF Core

    internal void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        Quantity = quantity;
    }

    internal void UpdatePrice(Money newPrice)
    {
        UnitPrice = newPrice;
    }
}

public class SavedForLaterItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime SavedAt { get; private set; }

    internal SavedForLaterItem(
        Guid id,
        Guid productId,
        string productName,
        Money unitPrice,
        string? imageUrl)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        ImageUrl = imageUrl;
        SavedAt = DateTime.UtcNow;
    }

    private SavedForLaterItem() { } // EF Core
}

public enum CartStatus
{
    Active = 1,
    CheckingOut = 2,
    Converted = 3,
    Abandoned = 4,
    Expired = 5
}
