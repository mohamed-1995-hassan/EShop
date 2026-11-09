using EShop.Domain.Aggregates.CatalogAggregate;

namespace EShop.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetFeaturedAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProductRecommendationRepository
{
    Task<ProductRecommendation?> GetByProductIdAsync(Guid productId, RecommendationType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductRecommendation>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductRecommendation recommendation, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductRecommendation recommendation, CancellationToken cancellationToken = default);
}

public interface ISearchQueryRepository
{
    Task<IReadOnlyList<SearchQuery>> GetPopularSearchesAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchQuery>> GetRecentByCustomerAsync(Guid customerId, int count, CancellationToken cancellationToken = default);
    Task AddAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
