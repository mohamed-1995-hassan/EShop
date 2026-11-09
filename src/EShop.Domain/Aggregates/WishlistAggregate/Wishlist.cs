using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.WishlistAggregate;

/// <summary>
/// Wishlist Aggregate - Customer's saved products for later purchase
/// 
/// BUSINESS RULES:
/// - One default wishlist per customer
/// - Multiple named wishlists allowed (gift registry, etc.)
/// - Price drop notifications
/// - Shareable wishlists with privacy controls
/// - Maximum 100 items per wishlist
/// </summary>
public class Wishlist : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public WishlistType Type { get; private set; }
    public WishlistPrivacy Privacy { get; private set; }
    public bool IsDefault { get; private set; }
    public string? ShareToken { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastModifiedAt { get; private set; }

    private readonly List<WishlistItem> _items = new();
    public IReadOnlyCollection<WishlistItem> Items => _items.AsReadOnly();

    private readonly List<WishlistCollaborator> _collaborators = new();
    public IReadOnlyCollection<WishlistCollaborator> Collaborators => _collaborators.AsReadOnly();

    public int ItemCount => _items.Count;
    public bool IsShareable => Privacy != WishlistPrivacy.Private;

    private Wishlist() { }

    public static Wishlist CreateDefault(Guid customerId)
    {
        return new Wishlist
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Name = "My Wishlist",
            Type = WishlistType.Personal,
            Privacy = WishlistPrivacy.Private,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };
    }

    public static Wishlist Create(
        Guid customerId,
        string name,
        WishlistType type,
        WishlistPrivacy privacy = WishlistPrivacy.Private,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Wishlist name is required");

        var wishlist = new Wishlist
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Name = name,
            Description = description,
            Type = type,
            Privacy = privacy,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        if (privacy != WishlistPrivacy.Private)
            wishlist.GenerateShareToken();

        return wishlist;
    }

    public static Wishlist CreateGiftRegistry(
        Guid customerId,
        string name,
        string eventType,
        DateTime eventDate,
        string? description = null)
    {
        var wishlist = Create(customerId, name, WishlistType.GiftRegistry, WishlistPrivacy.SharedWithLink, description);
        wishlist.Description = $"{eventType} - {eventDate:d}\n{description}";
        return wishlist;
    }

    #region Item Management

    public WishlistItem AddItem(
        Guid productId,
        string productName,
        Money currentPrice,
        string? imageUrl = null,
        int desiredQuantity = 1,
        string? notes = null,
        int? priority = null)
    {
        if (_items.Count >= 100)
            throw new DomainException("Wishlist cannot contain more than 100 items");

        if (_items.Any(i => i.ProductId == productId))
            throw new DomainException("Product already in wishlist");

        var item = new WishlistItem(
            Guid.NewGuid(),
            productId,
            productName,
            currentPrice,
            imageUrl,
            desiredQuantity,
            notes,
            priority ?? _items.Count + 1);

        _items.Add(item);
        UpdateModified();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in wishlist");

        _items.Remove(item);
        UpdateModified();
    }

    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in wishlist");

        item.UpdateDesiredQuantity(quantity);
        UpdateModified();
    }

    public void UpdateItemPriority(Guid itemId, int priority)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in wishlist");

        item.UpdatePriority(priority);
        UpdateModified();
    }

    public void UpdateItemNotes(Guid itemId, string? notes)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in wishlist");

        item.UpdateNotes(notes);
        UpdateModified();
    }

    public void MarkItemAsPurchased(Guid itemId, Guid? purchasedBy = null)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found in wishlist");

        item.MarkAsPurchased(purchasedBy);
        UpdateModified();
    }

    public void UpdateItemPrice(Guid productId, Money newPrice)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.UpdateCurrentPrice(newPrice);
            UpdateModified();
        }
    }

    public IEnumerable<WishlistItem> GetItemsWithPriceDrop()
    {
        return _items.Where(i => i.HasPriceDrop && !i.IsPurchased);
    }

    #endregion

    #region Wishlist Settings

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Wishlist name is required");

        Name = name;
        UpdateModified();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdateModified();
    }

    public void SetPrivacy(WishlistPrivacy privacy)
    {
        Privacy = privacy;
        
        if (privacy != WishlistPrivacy.Private && string.IsNullOrEmpty(ShareToken))
            GenerateShareToken();

        UpdateModified();
    }

    public void RegenerateShareToken()
    {
        if (Privacy == WishlistPrivacy.Private)
            throw new DomainException("Cannot generate share token for private wishlist");

        GenerateShareToken();
        UpdateModified();
    }

    private void GenerateShareToken()
    {
        ShareToken = Guid.NewGuid().ToString("N")[..12];
    }

    #endregion

    #region Collaborators (for shared wishlists)

    public void AddCollaborator(Guid userId, string name, CollaboratorRole role = CollaboratorRole.Viewer)
    {
        if (Privacy == WishlistPrivacy.Private)
            throw new DomainException("Cannot add collaborators to private wishlist");

        if (_collaborators.Any(c => c.UserId == userId))
            throw new DomainException("User is already a collaborator");

        _collaborators.Add(new WishlistCollaborator(Guid.NewGuid(), userId, name, role));
        UpdateModified();
    }

    public void RemoveCollaborator(Guid userId)
    {
        var collaborator = _collaborators.FirstOrDefault(c => c.UserId == userId)
            ?? throw new DomainException("Collaborator not found");

        _collaborators.Remove(collaborator);
        UpdateModified();
    }

    public void UpdateCollaboratorRole(Guid userId, CollaboratorRole role)
    {
        var collaborator = _collaborators.FirstOrDefault(c => c.UserId == userId)
            ?? throw new DomainException("Collaborator not found");

        collaborator.UpdateRole(role);
        UpdateModified();
    }

    #endregion

    private void UpdateModified() => LastModifiedAt = DateTime.UtcNow;
}

public class WishlistItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money PriceWhenAdded { get; private set; }
    public Money CurrentPrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public int DesiredQuantity { get; private set; }
    public string? Notes { get; private set; }
    public int Priority { get; private set; }
    public bool IsPurchased { get; private set; }
    public Guid? PurchasedBy { get; private set; }
    public DateTime? PurchasedAt { get; private set; }
    public DateTime AddedAt { get; private set; }
    public bool NotifyOnPriceDrop { get; private set; }

    public bool HasPriceDrop => CurrentPrice.Amount < PriceWhenAdded.Amount;
    public decimal PriceDropPercentage => HasPriceDrop 
        ? Math.Round((1 - CurrentPrice.Amount / PriceWhenAdded.Amount) * 100, 1) 
        : 0;

    internal WishlistItem(
        Guid id,
        Guid productId,
        string productName,
        Money currentPrice,
        string? imageUrl,
        int desiredQuantity,
        string? notes,
        int priority)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        PriceWhenAdded = currentPrice;
        CurrentPrice = currentPrice;
        ImageUrl = imageUrl;
        DesiredQuantity = desiredQuantity;
        Notes = notes;
        Priority = priority;
        IsPurchased = false;
        AddedAt = DateTime.UtcNow;
        NotifyOnPriceDrop = true;
    }

    private WishlistItem() { }

    internal void UpdateDesiredQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        DesiredQuantity = quantity;
    }

    internal void UpdatePriority(int priority) => Priority = priority;
    internal void UpdateNotes(string? notes) => Notes = notes;
    internal void UpdateCurrentPrice(Money newPrice) => CurrentPrice = newPrice;
    internal void EnablePriceDropNotification() => NotifyOnPriceDrop = true;
    internal void DisablePriceDropNotification() => NotifyOnPriceDrop = false;

    internal void MarkAsPurchased(Guid? purchasedBy)
    {
        IsPurchased = true;
        PurchasedBy = purchasedBy;
        PurchasedAt = DateTime.UtcNow;
    }
}

public class WishlistCollaborator : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public CollaboratorRole Role { get; private set; }
    public DateTime AddedAt { get; private set; }

    internal WishlistCollaborator(Guid id, Guid userId, string name, CollaboratorRole role)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Role = role;
        AddedAt = DateTime.UtcNow;
    }

    private WishlistCollaborator() { }

    internal void UpdateRole(CollaboratorRole role) => Role = role;
}

public enum WishlistType
{
    Personal = 1,
    GiftRegistry = 2,
    Wedding = 3,
    BabyShower = 4,
    Birthday = 5,
    Holiday = 6,
    SavedForLater = 7
}

public enum WishlistPrivacy
{
    Private = 1,
    SharedWithLink = 2,
    Public = 3
}

public enum CollaboratorRole
{
    Viewer = 1,
    Editor = 2,
    Admin = 3
}
