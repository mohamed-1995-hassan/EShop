using EShop.Domain.Aggregates.CatalogAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private static readonly ConcurrentDictionary<Guid, Category> _categories = new();

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_categories.GetValueOrDefault(id));

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(_categories.Values.FirstOrDefault(c => c.Slug == slug));

    public Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Category>>(_categories.Values.Where(c => c.IsRoot && c.IsActive).OrderBy(c => c.SortOrder).ToList());

    public Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Category>>(_categories.Values.Where(c => c.ParentCategoryId == parentId && c.IsActive).OrderBy(c => c.SortOrder).ToList());

    public Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Category>>(_categories.Values.Where(c => c.IsActive).ToList());

    public Task<IReadOnlyList<Category>> GetFeaturedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Category>>(_categories.Values.Where(c => c.IsFeatured && c.IsActive).ToList());

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _categories[category.Id] = category;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
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

public class ProductRecommendationRepository : IProductRecommendationRepository
{
    private static readonly ConcurrentDictionary<Guid, ProductRecommendation> _recommendations = new();

    public Task<ProductRecommendation?> GetByProductIdAsync(Guid productId, RecommendationType type, CancellationToken cancellationToken = default)
        => Task.FromResult(_recommendations.Values.FirstOrDefault(r => r.SourceProductId == productId && r.Type == type && !r.IsExpired));

    public Task<IReadOnlyList<ProductRecommendation>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProductRecommendation>>(_recommendations.Values.Where(r => r.SourceProductId == productId && !r.IsExpired).ToList());

    public Task AddAsync(ProductRecommendation recommendation, CancellationToken cancellationToken = default)
    {
        _recommendations[recommendation.Id] = recommendation;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ProductRecommendation recommendation, CancellationToken cancellationToken = default)
    {
        _recommendations[recommendation.Id] = recommendation;
        return Task.CompletedTask;
    }
}

public class SearchQueryRepository : ISearchQueryRepository
{
    private static readonly ConcurrentBag<SearchQuery> _queries = new();

    public Task<IReadOnlyList<SearchQuery>> GetPopularSearchesAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SearchQuery>>(_queries.GroupBy(q => q.Query).OrderByDescending(g => g.Count()).Take(count).Select(g => g.First()).ToList());

    public Task<IReadOnlyList<SearchQuery>> GetRecentByCustomerAsync(Guid customerId, int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SearchQuery>>(_queries.Where(q => q.CustomerId == customerId).OrderByDescending(q => q.SearchedAt).Take(count).ToList());

    public Task AddAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        _queries.Add(query);
        return Task.CompletedTask;
    }
}
