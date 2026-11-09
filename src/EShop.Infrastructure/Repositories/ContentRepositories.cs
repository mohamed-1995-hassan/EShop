using EShop.Domain.Aggregates.ContentAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

public class BannerRepository : IBannerRepository
{
    private static readonly ConcurrentDictionary<Guid, Banner> _banners = new();

    public Task<Banner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_banners.GetValueOrDefault(id));

    public Task<IReadOnlyList<Banner>> GetActiveByPositionAsync(BannerPosition position, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Banner>>(_banners.Values.Where(b => b.IsActive && b.Position == position).OrderBy(b => b.SortOrder).ToList());

    public Task<IReadOnlyList<Banner>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Banner>>(_banners.Values.ToList());

    public Task<IReadOnlyList<Banner>> GetScheduledAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Banner>>(_banners.Values.Where(b => b.StartDate.HasValue && b.StartDate > DateTime.UtcNow).ToList());

    public Task AddAsync(Banner banner, CancellationToken cancellationToken = default)
    {
        _banners[banner.Id] = banner;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Banner banner, CancellationToken cancellationToken = default)
    {
        _banners[banner.Id] = banner;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _banners.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

public class LandingPageRepository : ILandingPageRepository
{
    private static readonly ConcurrentDictionary<Guid, LandingPage> _pages = new();

    public Task<LandingPage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_pages.GetValueOrDefault(id));

    public Task<LandingPage?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(_pages.Values.FirstOrDefault(p => p.Slug == slug));

    public Task<IReadOnlyList<LandingPage>> GetPublishedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LandingPage>>(_pages.Values.Where(p => p.Status == PageStatus.Published).ToList());

    public Task<IReadOnlyList<LandingPage>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LandingPage>>(_pages.Values.Where(p => p.CampaignId == campaignId).ToList());

    public Task AddAsync(LandingPage page, CancellationToken cancellationToken = default)
    {
        _pages[page.Id] = page;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LandingPage page, CancellationToken cancellationToken = default)
    {
        _pages[page.Id] = page;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _pages.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

public class FaqRepository : IFaqRepository
{
    private static readonly ConcurrentDictionary<Guid, FaqCategory> _categories = new();

    public Task<FaqCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_categories.GetValueOrDefault(id));

    public Task<IReadOnlyList<FaqCategory>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<FaqCategory>>(_categories.Values.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList());

    public Task AddAsync(FaqCategory category, CancellationToken cancellationToken = default)
    {
        _categories[category.Id] = category;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FaqCategory category, CancellationToken cancellationToken = default)
    {
        _categories[category.Id] = category;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _categories.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

public class BlogPostRepository : IBlogPostRepository
{
    private static readonly ConcurrentDictionary<Guid, BlogPost> _posts = new();

    public Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_posts.GetValueOrDefault(id));

    public Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(_posts.Values.FirstOrDefault(p => p.Slug == slug));

    public Task<IReadOnlyList<BlogPost>> GetPublishedAsync(int skip, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BlogPost>>(_posts.Values.Where(p => p.Status == BlogPostStatus.Published).OrderByDescending(p => p.PublishedAt).Skip(skip).Take(take).ToList());

    public Task<IReadOnlyList<BlogPost>> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BlogPost>>(_posts.Values.Where(p => p.Tags.Contains(tag.ToLowerInvariant())).ToList());

    public Task<IReadOnlyList<BlogPost>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BlogPost>>(_posts.Values.Where(p => p.IsFeatured && p.Status == BlogPostStatus.Published).Take(count).ToList());

    public Task<IReadOnlyList<BlogPost>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BlogPost>>(_posts.Values.Where(p => p.AuthorId == authorId).ToList());

    public Task AddAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        _posts[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        _posts[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _posts.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
