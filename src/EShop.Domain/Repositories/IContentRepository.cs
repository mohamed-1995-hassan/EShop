using EShop.Domain.Aggregates.ContentAggregate;

namespace EShop.Domain.Repositories;

public interface IBannerRepository
{
    Task<Banner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Banner>> GetActiveByPositionAsync(BannerPosition position, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Banner>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Banner>> GetScheduledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Banner banner, CancellationToken cancellationToken = default);
    Task UpdateAsync(Banner banner, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ILandingPageRepository
{
    Task<LandingPage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LandingPage?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LandingPage>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LandingPage>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task AddAsync(LandingPage page, CancellationToken cancellationToken = default);
    Task UpdateAsync(LandingPage page, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IFaqRepository
{
    Task<FaqCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaqCategory>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(FaqCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(FaqCategory category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IBlogPostRepository
{
    Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogPost>> GetPublishedAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogPost>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogPost>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogPost>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default);
    Task AddAsync(BlogPost post, CancellationToken cancellationToken = default);
    Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
