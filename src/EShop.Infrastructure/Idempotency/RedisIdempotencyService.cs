using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EShop.Infrastructure.Idempotency;

/// <summary>
/// Redis-based idempotency service.
/// 
/// Uses Redis for distributed idempotency key storage with:
/// - Atomic check-and-set operations
/// - Automatic expiration
/// - Request hash validation (optional)
/// </summary>
public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisIdempotencyService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(24);

    // Lua script for atomic check-and-set
    private const string IdempotencyScript = @"
        local key = KEYS[1]
        local value = ARGV[1]
        local ttl = tonumber(ARGV[2])
        
        -- Check if key exists
        local existing = redis.call('GET', key)
        if existing then
            return existing
        end
        
        -- Set new value with TTL
        redis.call('SET', key, value, 'EX', ttl)
        return nil
    ";

    public RedisIdempotencyService(
        IConnectionMultiplexer redis,
        ILogger<RedisIdempotencyService> logger)
    {
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IdempotentResult<T>> ExecuteAsync<T>(
        string idempotencyKey,
        Func<Task<T>> operation,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = $"idempotency:{idempotencyKey}";
        var db = _redis.GetDatabase();
        var expiry = ttl ?? _defaultTtl;

        // Check for existing response
        var existingJson = await db.StringGetAsync(fullKey);
        if (existingJson.HasValue)
        {
            var record = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson!, _jsonOptions);
            if (record != null)
            {
                var cachedResponse = JsonSerializer.Deserialize<T>(record.ResponseJson, _jsonOptions);
                _logger.LogInformation("Returning cached response for idempotency key {Key}", idempotencyKey);
                return IdempotentResult<T>.FromCache(cachedResponse!, idempotencyKey, record.ProcessedAt);
            }
        }

        // Execute the operation
        _logger.LogDebug("Executing operation for idempotency key {Key}", idempotencyKey);
        var response = await operation();

        // Store the response
        var newRecord = new IdempotencyRecord
        {
            Key = idempotencyKey,
            ResponseType = typeof(T).FullName!,
            ResponseJson = JsonSerializer.Serialize(response, _jsonOptions),
            ProcessedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiry)
        };

        var recordJson = JsonSerializer.Serialize(newRecord, _jsonOptions);

        // Use atomic operation to prevent race conditions
        var result = await db.ScriptEvaluateAsync(
            IdempotencyScript,
            new RedisKey[] { fullKey },
            new RedisValue[] { recordJson, (int)expiry.TotalSeconds });

        if (!result.IsNull)
        {
            // Another request beat us - return their response
            var existingRecord = JsonSerializer.Deserialize<IdempotencyRecord>(result.ToString(), _jsonOptions);
            var existingResponse = JsonSerializer.Deserialize<T>(existingRecord!.ResponseJson, _jsonOptions);
            _logger.LogInformation("Race condition detected, returning existing response for {Key}", idempotencyKey);
            return IdempotentResult<T>.FromCache(existingResponse!, idempotencyKey, existingRecord.ProcessedAt);
        }

        return IdempotentResult<T>.Fresh(response, idempotencyKey);
    }

    public async Task<bool> ExistsAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"idempotency:{idempotencyKey}");
    }

    public async Task<T?> GetResponseAsync<T>(
        string idempotencyKey,
        CancellationToken cancellationToken = default) where T : class
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync($"idempotency:{idempotencyKey}");
        
        if (!json.HasValue) return null;

        var record = JsonSerializer.Deserialize<IdempotencyRecord>(json!, _jsonOptions);
        return record != null
            ? JsonSerializer.Deserialize<T>(record.ResponseJson, _jsonOptions)
            : null;
    }

    public async Task InvalidateAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"idempotency:{idempotencyKey}");
        _logger.LogDebug("Invalidated idempotency key {Key}", idempotencyKey);
    }
}
