using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.CatalogAggregate;

/// <summary>
/// Catalog Aggregate - Product categorization and search
/// 
/// BUSINESS RULES:
/// - Hierarchical category structure
/// - Faceted search with filters
/// - Product recommendations
/// - Category SEO metadata
/// - Featured/trending products
/// </summary>
public class Category : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? IconUrl { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public int Level { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int ProductCount { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? MetaKeywords { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Guid> _childCategoryIds = new();
    public IReadOnlyCollection<Guid> ChildCategoryIds => _childCategoryIds.AsReadOnly();

    private readonly List<CategoryAttribute> _attributes = new();
    public IReadOnlyCollection<CategoryAttribute> Attributes => _attributes.AsReadOnly();

    private readonly List<Guid> _featuredProductIds = new();
    public IReadOnlyCollection<Guid> FeaturedProductIds => _featuredProductIds.AsReadOnly();

    public string Path { get; private set; } = string.Empty;
    public bool IsRoot => ParentCategoryId == null;
    public bool HasChildren => _childCategoryIds.Any();

    private Category() { }

    public static Category CreateRoot(string name, string? description = null, string? imageUrl = null)
    {
        return Create(name, null, 0, description, imageUrl);
    }

    public static Category CreateChild(string name, Guid parentId, int level, string? description = null)
    {
        return Create(name, parentId, level, description, null);
    }

    private static Category Create(string name, Guid? parentId, int level, string? description, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required");

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = GenerateSlug(name),
            Description = description,
            ImageUrl = imageUrl,
            ParentCategoryId = parentId,
            Level = level,
            SortOrder = 0,
            IsActive = true,
            IsFeatured = false,
            ProductCount = 0,
            Path = name.ToLowerInvariant().Replace(" ", "-"),
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Hierarchy

    public void AddChildCategory(Guid childId)
    {
        if (!_childCategoryIds.Contains(childId))
            _childCategoryIds.Add(childId);
    }

    public void RemoveChildCategory(Guid childId)
    {
        _childCategoryIds.Remove(childId);
    }

    public void SetParent(Guid? parentId, int level, string path)
    {
        ParentCategoryId = parentId;
        Level = level;
        Path = path;
    }

    public void UpdatePath(string path)
    {
        Path = path;
    }

    #endregion

    #region Attributes (for faceted search)

    public CategoryAttribute AddAttribute(
        string name,
        AttributeType type,
        bool isFilterable = true,
        bool isSearchable = false)
    {
        if (_attributes.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Attribute already exists");

        var attribute = new CategoryAttribute(
            Guid.NewGuid(),
            name,
            type,
            isFilterable,
            isSearchable,
            _attributes.Count);

        _attributes.Add(attribute);
        return attribute;
    }

    public void RemoveAttribute(Guid attributeId)
    {
        var attribute = _attributes.FirstOrDefault(a => a.Id == attributeId);
        if (attribute != null)
        {
            _attributes.Remove(attribute);
            ReorderAttributes();
        }
    }

    public void AddAttributeOption(Guid attributeId, string value)
    {
        var attribute = _attributes.FirstOrDefault(a => a.Id == attributeId)
            ?? throw new DomainException("Attribute not found");

        attribute.AddOption(value);
    }

    private void ReorderAttributes()
    {
        for (int i = 0; i < _attributes.Count; i++)
            _attributes[i].SetSortOrder(i);
    }

    #endregion

    #region Products

    public void IncrementProductCount() => ProductCount++;
    public void DecrementProductCount() => ProductCount = Math.Max(0, ProductCount - 1);
    public void SetProductCount(int count) => ProductCount = Math.Max(0, count);

    public void AddFeaturedProduct(Guid productId)
    {
        if (_featuredProductIds.Count >= 10)
            throw new DomainException("Maximum 10 featured products allowed");

        if (!_featuredProductIds.Contains(productId))
            _featuredProductIds.Add(productId);
    }

    public void RemoveFeaturedProduct(Guid productId)
    {
        _featuredProductIds.Remove(productId);
    }

    public void ClearFeaturedProducts()
    {
        _featuredProductIds.Clear();
    }

    #endregion

    #region SEO

    public void SetSeoMetadata(string? title, string? description, string? keywords)
    {
        MetaTitle = title;
        MetaDescription = description;
        MetaKeywords = keywords;
    }

    public void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Slug is required");

        Slug = slug.ToLowerInvariant().Trim();
    }

    #endregion

    #region Configuration

    public void Update(string name, string? description, string? imageUrl, string? iconUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");

        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        IconUrl = iconUrl;
    }

    public void SetSortOrder(int order) => SortOrder = order;
    public void SetFeatured(bool featured) => IsFeatured = featured;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    #endregion

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and");
    }
}

public class CategoryAttribute : Entity<Guid>
{
    public string Name { get; private set; }
    public AttributeType Type { get; private set; }
    public bool IsFilterable { get; private set; }
    public bool IsSearchable { get; private set; }
    public int SortOrder { get; private set; }

    private readonly List<string> _options = new();
    public IReadOnlyCollection<string> Options => _options.AsReadOnly();

    internal CategoryAttribute(
        Guid id,
        string name,
        AttributeType type,
        bool isFilterable,
        bool isSearchable,
        int sortOrder)
    {
        Id = id;
        Name = name;
        Type = type;
        IsFilterable = isFilterable;
        IsSearchable = isSearchable;
        SortOrder = sortOrder;
    }

    private CategoryAttribute() { }

    internal void AddOption(string value)
    {
        if (!_options.Contains(value))
            _options.Add(value);
    }

    internal void RemoveOption(string value) => _options.Remove(value);
    internal void SetSortOrder(int order) => SortOrder = order;
}

/// <summary>
/// Product Recommendation Engine
/// </summary>
public class ProductRecommendation : AggregateRoot<Guid>
{
    public Guid SourceProductId { get; private set; }
    public RecommendationType Type { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private readonly List<RecommendedProduct> _recommendations = new();
    public IReadOnlyCollection<RecommendedProduct> Recommendations => _recommendations.AsReadOnly();

    private ProductRecommendation() { }

    public static ProductRecommendation Create(Guid sourceProductId, RecommendationType type, int expirationHours = 24)
    {
        return new ProductRecommendation
        {
            Id = Guid.NewGuid(),
            SourceProductId = sourceProductId,
            Type = type,
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(expirationHours)
        };
    }

    public void AddRecommendation(Guid productId, decimal score, string? reason = null)
    {
        if (_recommendations.Any(r => r.ProductId == productId))
            return;

        _recommendations.Add(new RecommendedProduct(
            Guid.NewGuid(),
            productId,
            score,
            reason,
            _recommendations.Count + 1));
    }

    public void ClearRecommendations() => _recommendations.Clear();

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}

public class RecommendedProduct : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public decimal Score { get; private set; }
    public string? Reason { get; private set; }
    public int Rank { get; private set; }

    internal RecommendedProduct(Guid id, Guid productId, decimal score, string? reason, int rank)
    {
        Id = id;
        ProductId = productId;
        Score = score;
        Reason = reason;
        Rank = rank;
    }

    private RecommendedProduct() { }
}

/// <summary>
/// Search Query for analytics
/// </summary>
public class SearchQuery : AggregateRoot<Guid>
{
    public string Query { get; private set; } = string.Empty;
    public int ResultCount { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? SessionId { get; private set; }
    public bool HasResults => ResultCount > 0;
    public DateTime SearchedAt { get; private set; }

    private readonly List<SearchFilter> _filters = new();
    public IReadOnlyCollection<SearchFilter> Filters => _filters.AsReadOnly();

    private SearchQuery() { }

    public static SearchQuery Create(string query, Guid? customerId, string? sessionId)
    {
        return new SearchQuery
        {
            Id = Guid.NewGuid(),
            Query = query.Trim(),
            CustomerId = customerId,
            SessionId = sessionId,
            ResultCount = 0,
            SearchedAt = DateTime.UtcNow
        };
    }

    public void AddFilter(string name, string value)
    {
        _filters.Add(new SearchFilter(name, value));
    }

    public void SetResultCount(int count) => ResultCount = count;
}

public record SearchFilter(string Name, string Value);

public enum AttributeType
{
    Text = 1,
    Number = 2,
    Boolean = 3,
    Select = 4,
    MultiSelect = 5,
    Range = 6,
    Color = 7,
    Size = 8
}

public enum RecommendationType
{
    FrequentlyBoughtTogether = 1,
    SimilarProducts = 2,
    ViewedTogether = 3,
    PersonalizedForYou = 4,
    Trending = 5,
    NewArrivals = 6,
    BestSellers = 7,
    RecentlyViewed = 8
}
