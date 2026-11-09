using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EShop.Infrastructure.Caching;

/// <summary>
/// Redis-based distributed cache implementation.
/// 
/// FEATURES:
/// - JSON serialization
/// - Absolute and sliding expiration
/// - Pattern-based key removal
/// - Batch operations
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(data, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting cache key {Key}", key);
            return null;
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT for {Key}", key);
            return cached;
        }

        _logger.LogDebug("Cache MISS for {Key}, computing value", key);
        var value = await factory();
        
        await SetAsync(key, value, options, cancellationToken);
        
        return value;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        options ??= CacheOptions.Default;

        var distributedOptions = new DistributedCacheEntryOptions();
        
        if (options.AbsoluteExpiration.HasValue)
        {
            distributedOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpiration;
        }
        
        if (options.SlidingExpiration.HasValue)
        {
            distributedOptions.SlidingExpiration = options.SlidingExpiration;
        }

        try
        {
            var data = JsonSerializer.Serialize(value, _jsonOptions);
            await _cache.SetStringAsync(key, data, distributedOptions, cancellationToken);
            
            _logger.LogDebug("Cached {Key} with expiry {Expiry}", 
                key, options.AbsoluteExpiration ?? options.SlidingExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length == 0) return;

            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys);
            
            _logger.LogDebug("Removed {Count} cache keys matching pattern {Pattern}", 
                keys.Length, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache keys by pattern {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetAsync(key, cancellationToken);
            return data != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cache key existence {Key}", key);
            return false;
        }
    }

    public async Task<bool> RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RefreshAsync(key, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing cache key {Key}", key);
            return false;
        }
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();
        var db = _redis.GetDatabase();
        var keyArray = keys.Select(k => (RedisKey)k).ToArray();

        try
        {
            var values = await db.StringGetAsync(keyArray);
            
            for (int i = 0; i < keyArray.Length; i++)
            {
                var key = keyArray[i].ToString();
                if (values[i].HasValue)
                {
                    result[key] = JsonSerializer.Deserialize<T>(values[i]!, _jsonOptions);
                }
                else
                {
                    result[key] = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting multiple cache keys");
        }

        return result;
    }

    public async Task SetManyAsync<T>(
        IDictionary<string, T> values,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        options ??= CacheOptions.Default;
        var db = _redis.GetDatabase();
        var expiry = options.AbsoluteExpiration ?? options.SlidingExpiration;

        try
        {
            var batch = db.CreateBatch();
            var tasks = new List<Task>();

            foreach (var (key, value) in values)
            {
                var data = JsonSerializer.Serialize(value, _jsonOptions);
                tasks.Add(batch.StringSetAsync(key, data, expiry));
            }

            batch.Execute();
            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Cached {Count} keys in batch", values.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting multiple cache keys");
        }
    }
}
