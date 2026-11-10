using EShop.Domain.Aggregates.ProductAggregate;
using EShop.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Caching;

/// <summary>
/// DECORATOR PATTERN FOR CACHED REPOSITORIES
/// 
/// Wraps repository operations with caching:
/// - Cache-Aside pattern for reads
/// - Cache invalidation on writes
/// - Configurable TTL per entity type
/// 
/// Benefits:
/// - Transparent to consumers
/// - Reduces database load
/// - Consistent cache management
/// </summary>
public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedProductRepository> _logger;

    private static readonly CacheOptions ProductCacheOptions = new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(15),
        Priority = CachePriority.High
    };

    private static readonly CacheOptions ListCacheOptions = new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(5),
        Priority = CachePriority.Normal
    };

    public CachedProductRepository(
        IProductRepository inner,
        ICacheService cache,
        ILogger<CachedProductRepository> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Product(id);
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _inner.GetByIdAsync(id, cancellationToken),
            ProductCacheOptions,
            cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ProductBySku(sku);
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _inner.GetBySkuAsync(sku, cancellationToken),
            ProductCacheOptions,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        string category, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ProductsByCategory(category);
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _inner.GetByCategoryAsync(category, cancellationToken),
            ListCacheOptions,
            cancellationToken) ?? new List<Product>();
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        // Don't cache low stock - needs to be real-time
        return await _inner.GetLowStockAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        // Search results - short cache
        var cacheKey = $"eshop:products:search:{searchTerm.ToLowerInvariant().GetHashCode()}";
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _inner.SearchAsync(searchTerm, cancellationToken),
            CacheOptions.ShortLived,
            cancellationToken) ?? new List<Product>();
    }

    public async Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        // Don't cache existence checks - need real-time for create operations
        return await _inner.ExistsAsync(sku, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _inner.AddAsync(product, cancellationToken);
        
        // Invalidate list caches
        await InvalidateProductListCachesAsync();
        
        _logger.LogDebug("Added product {ProductId} and invalidated list caches", product.Id);
    }

    public void Update(Product product)
    {
        _inner.Update(product);
        
        // Note: Cache invalidation happens in SaveChangesAsync
        _logger.LogDebug("Updated product {ProductId}", product.Id);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _inner.SaveChangesAsync(cancellationToken);
        
        // Invalidate all product caches on save
        await InvalidateProductListCachesAsync();
        
        return result;
    }

    public async Task<int> SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
    {
        var result = await _inner.SaveChangesWithOutboxAsync(cancellationToken);
        
        // Invalidate all product caches on save
        await InvalidateProductListCachesAsync();
        
        return result;
    }

    private async Task InvalidateProductListCachesAsync()
    {
        await _cache.RemoveByPatternAsync("eshop:products:*");
    }
}

/// <summary>
/// Query caching decorator for read-heavy queries
/// </summary>
public class CachedQueryService<TQuery, TResult> where TResult : class
{
    private readonly ICacheService _cache;
    private readonly ILogger _logger;
    private readonly Func<TQuery, string> _keyGenerator;
    private readonly CacheOptions _options;

    public CachedQueryService(
        ICacheService cache,
        ILogger logger,
        Func<TQuery, string> keyGenerator,
        CacheOptions? options = null)
    {
        _cache = cache;
        _logger = logger;
        _keyGenerator = keyGenerator;
        _options = options ?? CacheOptions.Default;
    }

    public async Task<TResult> ExecuteAsync(
        TQuery query,
        Func<TQuery, Task<TResult>> executor,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator(query);
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            () => executor(query),
            _options,
            cancellationToken);
    }

    public async Task InvalidateAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator(query);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }
}
