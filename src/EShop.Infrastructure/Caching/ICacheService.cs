namespace EShop.Infrastructure.Caching;

/// <summary>
/// DISTRIBUTED CACHE SERVICE
/// 
/// Provides caching for:
/// - Product catalogs (high read, low write)
/// - Customer sessions
/// - Rate limiting counters
/// - Temporary data storage
/// 
/// CACHE PATTERNS:
/// - Cache-Aside: Application manages cache
/// - Write-Through: Cache + DB updated together
/// - Write-Behind: Cache updated, DB async
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached value
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Get cached value or compute and cache
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set cached value
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove cached value
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all keys matching pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if key exists
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh expiration of existing key
    /// </summary>
    Task<bool> RefreshAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple values
    /// </summary>
    Task<IDictionary<string, T?>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set multiple values
    /// </summary>
    Task SetManyAsync<T>(
        IDictionary<string, T> values,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Cache configuration options
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Absolute expiration time
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Sliding expiration - resets on access
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Cache tags for grouped invalidation
    /// </summary>
    public ICollection<string>? Tags { get; set; }

    /// <summary>
    /// Priority for eviction
    /// </summary>
    public CachePriority Priority { get; set; } = CachePriority.Normal;

    public static CacheOptions Default => new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(5),
        Priority = CachePriority.Normal
    };

    public static CacheOptions LongLived => new()
    {
        AbsoluteExpiration = TimeSpan.FromHours(1),
        Priority = CachePriority.High
    };

    public static CacheOptions ShortLived => new()
    {
        AbsoluteExpiration = TimeSpan.FromSeconds(30),
        Priority = CachePriority.Low
    };

    public static CacheOptions WithSliding(TimeSpan sliding) => new()
    {
        SlidingExpiration = sliding,
        Priority = CachePriority.Normal
    };
}

public enum CachePriority
{
    Low,
    Normal,
    High,
    NeverRemove
}

/// <summary>
/// Cache key builder for consistent key generation
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "eshop";

    public static string Product(Guid productId) => $"{Prefix}:product:{productId}";
    public static string ProductBySku(string sku) => $"{Prefix}:product:sku:{sku}";
    public static string ProductList(int page, int size) => $"{Prefix}:products:list:{page}:{size}";
    public static string ProductsByCategory(string category) => $"{Prefix}:products:category:{category}";
    
    public static string Customer(Guid customerId) => $"{Prefix}:customer:{customerId}";
    public static string CustomerOrders(Guid customerId) => $"{Prefix}:customer:{customerId}:orders";
    
    public static string Order(Guid orderId) => $"{Prefix}:order:{orderId}";
    public static string OrderStatus(Guid orderId) => $"{Prefix}:order:{orderId}:status";
    
    public static string IdempotencyKey(string key) => $"{Prefix}:idempotency:{key}";
    public static string RateLimit(string clientId, string endpoint) => $"{Prefix}:ratelimit:{clientId}:{endpoint}";
    
    public static string Session(string sessionId) => $"{Prefix}:session:{sessionId}";
}
