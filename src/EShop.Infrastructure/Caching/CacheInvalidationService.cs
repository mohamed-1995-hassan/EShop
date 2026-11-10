using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Caching;

/// <summary>
/// CACHE INVALIDATION SERVICE
/// 
/// Centralized cache invalidation for:
/// - Entity updates
/// - Bulk operations
/// - Related data changes
/// 
/// Patterns supported:
/// - Direct invalidation (single key)
/// - Pattern invalidation (wildcard)
/// - Tag-based invalidation (grouped keys)
/// - Cascading invalidation (related entities)
/// </summary>
public interface ICacheInvalidationService
{
    // Product-related
    Task InvalidateProductAsync(Guid productId, string? sku = null, CancellationToken ct = default);
    Task InvalidateProductListsAsync(CancellationToken ct = default);
    Task InvalidateAllProductsAsync(CancellationToken ct = default);
    
    // Customer-related
    Task InvalidateCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task InvalidateCustomerOrdersAsync(Guid customerId, CancellationToken ct = default);
    
    // Order-related
    Task InvalidateOrderAsync(Guid orderId, CancellationToken ct = default);
    
    // Category-related
    Task InvalidateCategoryAsync(string category, CancellationToken ct = default);
    Task InvalidateAllCategoriesAsync(CancellationToken ct = default);
    
    // Bulk operations
    Task InvalidateAllAsync(CancellationToken ct = default);
}

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheService cache,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    // PRODUCT CACHE INVALIDATION
    // ═══════════════════════════════════════════════════════════════════

    public async Task InvalidateProductAsync(Guid productId, string? sku = null, CancellationToken ct = default)
    {
        var tasks = new List<Task>
        {
            _cache.RemoveAsync(CacheKeys.Product(productId), ct)
        };

        if (!string.IsNullOrEmpty(sku))
        {
            tasks.Add(_cache.RemoveAsync(CacheKeys.ProductBySku(sku), ct));
        }

        await Task.WhenAll(tasks);
        
        // Also invalidate lists that might contain this product
        await InvalidateProductListsAsync(ct);
        
        _logger.LogDebug("Invalidated cache for product {ProductId}", productId);
    }

    public async Task InvalidateProductListsAsync(CancellationToken ct = default)
    {
        await _cache.RemoveByPatternAsync("eshop:products:*", ct);
        _logger.LogDebug("Invalidated all product list caches");
    }

    public async Task InvalidateAllProductsAsync(CancellationToken ct = default)
    {
        await _cache.RemoveByPatternAsync("eshop:product*", ct);
        _logger.LogDebug("Invalidated all product caches");
    }

    // ═══════════════════════════════════════════════════════════════════
    // CUSTOMER CACHE INVALIDATION
    // ═══════════════════════════════════════════════════════════════════

    public async Task InvalidateCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(CacheKeys.Customer(customerId), ct);
        _logger.LogDebug("Invalidated cache for customer {CustomerId}", customerId);
    }

    public async Task InvalidateCustomerOrdersAsync(Guid customerId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(CacheKeys.CustomerOrders(customerId), ct);
        _logger.LogDebug("Invalidated orders cache for customer {CustomerId}", customerId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // ORDER CACHE INVALIDATION
    // ═══════════════════════════════════════════════════════════════════

    public async Task InvalidateOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var tasks = new[]
        {
            _cache.RemoveAsync(CacheKeys.Order(orderId), ct),
            _cache.RemoveAsync(CacheKeys.OrderStatus(orderId), ct)
        };

        await Task.WhenAll(tasks);
        _logger.LogDebug("Invalidated cache for order {OrderId}", orderId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // CATEGORY CACHE INVALIDATION
    // ═══════════════════════════════════════════════════════════════════

    public async Task InvalidateCategoryAsync(string category, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(CacheKeys.ProductsByCategory(category), ct);
        _logger.LogDebug("Invalidated cache for category {Category}", category);
    }

    public async Task InvalidateAllCategoriesAsync(CancellationToken ct = default)
    {
        await _cache.RemoveByPatternAsync("eshop:products:category:*", ct);
        await _cache.RemoveByPatternAsync("eshop:category:*", ct);
        _logger.LogDebug("Invalidated all category caches");
    }

    // ═══════════════════════════════════════════════════════════════════
    // BULK INVALIDATION
    // ═══════════════════════════════════════════════════════════════════

    public async Task InvalidateAllAsync(CancellationToken ct = default)
    {
        await _cache.RemoveByPatternAsync("eshop:*", ct);
        _logger.LogWarning("Invalidated ALL caches");
    }
}

/// <summary>
/// Extended cache keys for additional entities
/// </summary>
public static class CacheKeysExtended
{
    private const string Prefix = "eshop";

    // Inventory
    public static string InventoryStock(Guid productId) => $"{Prefix}:inventory:stock:{productId}";
    public static string InventoryReservation(Guid orderId) => $"{Prefix}:inventory:reservation:{orderId}";
    
    // Cart
    public static string Cart(Guid customerId) => $"{Prefix}:cart:{customerId}";
    public static string CartItems(Guid cartId) => $"{Prefix}:cart:{cartId}:items";
    
    // Promotions
    public static string Promotion(string code) => $"{Prefix}:promotion:{code.ToLowerInvariant()}";
    public static string ActivePromotions() => $"{Prefix}:promotions:active";
    
    // Categories
    public static string Category(Guid categoryId) => $"{Prefix}:category:{categoryId}";
    public static string CategoryTree() => $"{Prefix}:categories:tree";
    
    // Search
    public static string SearchResults(string query, int page, int size) => 
        $"{Prefix}:search:{query.ToLowerInvariant().GetHashCode()}:{page}:{size}";
    
    // User sessions
    public static string UserPermissions(Guid userId) => $"{Prefix}:user:{userId}:permissions";
    public static string UserRoles(Guid userId) => $"{Prefix}:user:{userId}:roles";
    
    // Configuration
    public static string SystemSettings() => $"{Prefix}:settings:system";
    public static string TaxRates(string country) => $"{Prefix}:settings:tax:{country.ToLowerInvariant()}";
    public static string ShippingRates(string country) => $"{Prefix}:settings:shipping:{country.ToLowerInvariant()}";
}
