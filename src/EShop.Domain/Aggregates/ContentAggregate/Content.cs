using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.ContentAggregate;

/// <summary>
/// Content Management Aggregate - CMS for e-commerce
/// 
/// BUSINESS RULES:
/// - Banners with scheduling and targeting
/// - Landing pages for campaigns
/// - FAQ management
/// - Blog posts for SEO
/// - Dynamic content blocks
/// </summary>
public class Banner : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public string? MobileImageUrl { get; private set; }
    public string? LinkUrl { get; private set; }
    public BannerPosition Position { get; private set; }
    public BannerStatus Status { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public int ClickCount { get; private set; }
    public int ImpressionCount { get; private set; }
    public decimal ClickThroughRate => ImpressionCount > 0 ? (decimal)ClickCount / ImpressionCount * 100 : 0;
    public DateTime CreatedAt { get; private set; }

    private readonly List<string> _targetDevices = new();
    public IReadOnlyCollection<string> TargetDevices => _targetDevices.AsReadOnly();

    private readonly List<Guid> _targetCustomerSegments = new();
    public IReadOnlyCollection<Guid> TargetCustomerSegments => _targetCustomerSegments.AsReadOnly();

    public bool IsActive => Status == BannerStatus.Active && IsWithinSchedule;
    public bool IsWithinSchedule =>
        (!StartDate.HasValue || DateTime.UtcNow >= StartDate.Value) &&
        (!EndDate.HasValue || DateTime.UtcNow <= EndDate.Value);

    private Banner() { }

    public static Banner Create(
        string name,
        string title,
        string imageUrl,
        BannerPosition position,
        string? linkUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Banner name is required");
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new DomainException("Image URL is required");

        return new Banner
        {
            Id = Guid.NewGuid(),
            Name = name,
            Title = title,
            ImageUrl = imageUrl,
            LinkUrl = linkUrl,
            Position = position,
            Status = BannerStatus.Draft,
            SortOrder = 0,
            ClickCount = 0,
            ImpressionCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string? subtitle, string imageUrl, string? mobileImageUrl, string? linkUrl)
    {
        Title = title;
        Subtitle = subtitle;
        ImageUrl = imageUrl;
        MobileImageUrl = mobileImageUrl;
        LinkUrl = linkUrl;
    }

    public void Schedule(DateTime? start, DateTime? end)
    {
        if (start.HasValue && end.HasValue && start.Value >= end.Value)
            throw new DomainException("End date must be after start date");

        StartDate = start;
        EndDate = end;
    }

    public void SetTargetDevices(params string[] devices)
    {
        _targetDevices.Clear();
        _targetDevices.AddRange(devices);
    }

    public void SetTargetSegments(params Guid[] segmentIds)
    {
        _targetCustomerSegments.Clear();
        _targetCustomerSegments.AddRange(segmentIds);
    }

    public void RecordImpression() => ImpressionCount++;
    public void RecordClick() => ClickCount++;
    public void SetSortOrder(int order) => SortOrder = order;
    public void Publish() => Status = BannerStatus.Active;
    public void Unpublish() => Status = BannerStatus.Inactive;
    public void Archive() => Status = BannerStatus.Archived;
}

/// <summary>
/// Landing Page for campaigns
/// </summary>
public class LandingPage : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public PageStatus Status { get; private set; }
    public string? TemplateId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public int ViewCount { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<ContentBlock> _blocks = new();
    public IReadOnlyCollection<ContentBlock> Blocks => _blocks.AsReadOnly();

    private LandingPage() { }

    public static LandingPage Create(string title, string slug, string? templateId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Page title is required");

        return new LandingPage
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug.ToLowerInvariant().Trim(),
            TemplateId = templateId,
            Status = PageStatus.Draft,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public ContentBlock AddBlock(BlockType type, string content, int position)
    {
        var block = new ContentBlock(Guid.NewGuid(), type, content, position);
        _blocks.Add(block);
        return block;
    }

    public void RemoveBlock(Guid blockId)
    {
        var block = _blocks.FirstOrDefault(b => b.Id == blockId);
        if (block != null)
        {
            _blocks.Remove(block);
            ReorderBlocks();
        }
    }

    public void ReorderBlock(Guid blockId, int newPosition)
    {
        var block = _blocks.FirstOrDefault(b => b.Id == blockId)
            ?? throw new DomainException("Block not found");

        _blocks.Remove(block);
        _blocks.Insert(Math.Min(newPosition, _blocks.Count), block);
        ReorderBlocks();
    }

    private void ReorderBlocks()
    {
        for (int i = 0; i < _blocks.Count; i++)
            _blocks[i].SetPosition(i);
    }

    public void SetSeoMetadata(string? metaTitle, string? metaDescription)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
    }

    public void RecordView() => ViewCount++;

    public void Publish()
    {
        Status = PageStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void Unpublish() => Status = PageStatus.Draft;
    public void Archive() => Status = PageStatus.Archived;

    public void Update(string title, string? description)
    {
        Title = title;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class ContentBlock : Entity<Guid>
{
    public BlockType Type { get; private set; }
    public string Content { get; private set; }
    public string? Settings { get; private set; }
    public int Position { get; private set; }
    public bool IsVisible { get; private set; }

    internal ContentBlock(Guid id, BlockType type, string content, int position)
    {
        Id = id;
        Type = type;
        Content = content;
        Position = position;
        IsVisible = true;
    }

    private ContentBlock() { }

    public void UpdateContent(string content) => Content = content;
    public void UpdateSettings(string settings) => Settings = settings;
    internal void SetPosition(int position) => Position = position;
    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
}

/// <summary>
/// FAQ Management
/// </summary>
public class FaqCategory : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<FaqItem> _items = new();
    public IReadOnlyCollection<FaqItem> Items => _items.AsReadOnly();

    private FaqCategory() { }

    public static FaqCategory Create(string name, string? description = null)
    {
        return new FaqCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            SortOrder = 0,
            IsActive = true
        };
    }

    public FaqItem AddItem(string question, string answer)
    {
        var item = new FaqItem(Guid.NewGuid(), question, answer, _items.Count);
        _items.Add(item);
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            ReorderItems();
        }
    }

    public void UpdateItem(Guid itemId, string question, string answer)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("FAQ item not found");

        item.Update(question, answer);
    }

    private void ReorderItems()
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].SetSortOrder(i);
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetSortOrder(int order) => SortOrder = order;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class FaqItem : Entity<Guid>
{
    public string Question { get; private set; }
    public string Answer { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public int ViewCount { get; private set; }
    public int HelpfulCount { get; private set; }
    public int NotHelpfulCount { get; private set; }

    internal FaqItem(Guid id, string question, string answer, int sortOrder)
    {
        Id = id;
        Question = question;
        Answer = answer;
        SortOrder = sortOrder;
        IsActive = true;
        ViewCount = 0;
        HelpfulCount = 0;
        NotHelpfulCount = 0;
    }

    private FaqItem() { }

    internal void Update(string question, string answer)
    {
        Question = question;
        Answer = answer;
    }

    internal void SetSortOrder(int order) => SortOrder = order;
    public void RecordView() => ViewCount++;
    public void MarkHelpful() => HelpfulCount++;
    public void MarkNotHelpful() => NotHelpfulCount++;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Blog Post for content marketing/SEO
/// </summary>
public class BlogPost : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? FeaturedImageUrl { get; private set; }
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public BlogPostStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    public int ViewCount { get; private set; }
    public int CommentCount { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<string> _tags = new();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    private readonly List<Guid> _relatedProductIds = new();
    public IReadOnlyCollection<Guid> RelatedProductIds => _relatedProductIds.AsReadOnly();

    private BlogPost() { }

    public static BlogPost Create(string title, string content, Guid authorId, string authorName)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required");

        return new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = GenerateSlug(title),
            Excerpt = content.Length > 200 ? content[..200] + "..." : content,
            Content = content,
            AuthorId = authorId,
            AuthorName = authorName,
            Status = BlogPostStatus.Draft,
            IsFeatured = false,
            ViewCount = 0,
            CommentCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string excerpt, string content, string? featuredImageUrl)
    {
        Title = title;
        Excerpt = excerpt;
        Content = content;
        FeaturedImageUrl = featuredImageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTags(params string[] tags)
    {
        _tags.Clear();
        _tags.AddRange(tags.Select(t => t.ToLowerInvariant().Trim()));
    }

    public void SetRelatedProducts(params Guid[] productIds)
    {
        _relatedProductIds.Clear();
        _relatedProductIds.AddRange(productIds);
    }

    public void SetSeoMetadata(string? metaTitle, string? metaDescription)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
    }

    public void Publish()
    {
        Status = BlogPostStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void Unpublish() => Status = BlogPostStatus.Draft;
    public void Archive() => Status = BlogPostStatus.Archived;
    public void SetFeatured(bool featured) => IsFeatured = featured;
    public void RecordView() => ViewCount++;
    public void IncrementCommentCount() => CommentCount++;

    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and");
    }
}

public enum BannerPosition
{
    HomeHero = 1,
    HomeSidebar = 2,
    CategoryTop = 3,
    ProductSidebar = 4,
    CartPage = 5,
    CheckoutPage = 6,
    Footer = 7,
    Popup = 8
}

public enum BannerStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Archived = 4
}

public enum PageStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum BlockType
{
    Text = 1,
    Image = 2,
    Video = 3,
    ProductCarousel = 4,
    CategoryGrid = 5,
    Banner = 6,
    Html = 7,
    Form = 8,
    Testimonial = 9,
    Countdown = 10
}

public enum BlogPostStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
