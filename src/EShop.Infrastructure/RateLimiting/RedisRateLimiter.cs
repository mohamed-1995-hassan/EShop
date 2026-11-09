using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EShop.Infrastructure.RateLimiting;

/// <summary>
/// Redis-based distributed rate limiter.
/// 
/// Uses Lua scripts for atomic operations.
/// Supports multiple algorithms for different use cases.
/// </summary>
public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimiter> _logger;

    // Lua script for sliding window rate limiting (atomic)
    private const string SlidingWindowScript = @"
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])
        local limit = tonumber(ARGV[3])
        local consume = tonumber(ARGV[4])
        
        -- Remove old entries outside the window
        redis.call('ZREMRANGEBYSCORE', key, 0, now - window)
        
        -- Get current count
        local current = redis.call('ZCARD', key)
        
        if consume == 1 then
            if current < limit then
                -- Add new entry with current timestamp
                redis.call('ZADD', key, now, now .. ':' .. math.random())
                redis.call('EXPIRE', key, math.ceil(window / 1000))
                return {1, limit - current - 1, limit}
            else
                return {0, 0, limit}
            end
        else
            return {current < limit and 1 or 0, limit - current, limit}
        end
    ";

    // Lua script for fixed window rate limiting
    private const string FixedWindowScript = @"
        local key = KEYS[1]
        local limit = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])
        local consume = tonumber(ARGV[3])
        
        local current = tonumber(redis.call('GET', key) or '0')
        
        if consume == 1 then
            if current < limit then
                current = redis.call('INCR', key)
                if current == 1 then
                    redis.call('EXPIRE', key, window)
                end
                return {1, limit - current, limit}
            else
                return {0, 0, limit}
            end
        else
            return {current < limit and 1 or 0, limit - current, limit}
        end
    ";

    // Lua script for token bucket
    private const string TokenBucketScript = @"
        local key = KEYS[1]
        local limit = tonumber(ARGV[1])
        local refill_rate = tonumber(ARGV[2])
        local now = tonumber(ARGV[3])
        local consume = tonumber(ARGV[4])
        
        local bucket = redis.call('HMGET', key, 'tokens', 'last_update')
        local tokens = tonumber(bucket[1]) or limit
        local last_update = tonumber(bucket[2]) or now
        
        -- Refill tokens based on time elapsed
        local elapsed = (now - last_update) / 1000
        tokens = math.min(limit, tokens + (elapsed * refill_rate))
        
        if consume == 1 then
            if tokens >= 1 then
                tokens = tokens - 1
                redis.call('HMSET', key, 'tokens', tokens, 'last_update', now)
                redis.call('EXPIRE', key, math.ceil(limit / refill_rate) + 1)
                return {1, math.floor(tokens), limit}
            else
                return {0, 0, limit}
            end
        else
            return {tokens >= 1 and 1 or 0, math.floor(tokens), limit}
        end
    ";

    public RedisRateLimiter(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckAsync(
        string key,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteRateLimitAsync(key, policy, consume: true);
    }

    public async Task<RateLimitResult> PeekAsync(
        string key,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteRateLimitAsync(key, policy, consume: false);
    }

    private async Task<RateLimitResult> ExecuteRateLimitAsync(
        string key,
        RateLimitPolicy policy,
        bool consume)
    {
        var db = _redis.GetDatabase();
        var fullKey = $"ratelimit:{policy.Name}:{key}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            RedisResult result;

            switch (policy.Algorithm)
            {
                case RateLimitAlgorithm.SlidingWindow:
                    result = await db.ScriptEvaluateAsync(
                        SlidingWindowScript,
                        new RedisKey[] { fullKey },
                        new RedisValue[] { now, (long)policy.Window.TotalMilliseconds, policy.Limit, consume ? 1 : 0 });
                    break;

                case RateLimitAlgorithm.FixedWindow:
                    result = await db.ScriptEvaluateAsync(
                        FixedWindowScript,
                        new RedisKey[] { fullKey },
                        new RedisValue[] { policy.Limit, (int)policy.Window.TotalSeconds, consume ? 1 : 0 });
                    break;

                case RateLimitAlgorithm.TokenBucket:
                    var refillRate = policy.Limit / policy.Window.TotalSeconds;
                    result = await db.ScriptEvaluateAsync(
                        TokenBucketScript,
                        new RedisKey[] { fullKey },
                        new RedisValue[] { policy.BurstLimit ?? policy.Limit, refillRate, now, consume ? 1 : 0 });
                    break;

                default:
                    throw new NotSupportedException($"Algorithm {policy.Algorithm} not supported");
            }

            var values = (RedisResult[])result!;
            var isAllowed = (int)values[0] == 1;
            var remaining = (int)values[1];
            var limit = (int)values[2];
            var resetAt = DateTime.UtcNow.Add(policy.Window);

            if (isAllowed)
            {
                return RateLimitResult.Allowed(remaining, limit, resetAt);
            }
            else
            {
                var retryAfter = CalculateRetryAfter(policy);
                _logger.LogWarning("Rate limit exceeded for {Key} with policy {Policy}", 
                    key, policy.Name);
                return RateLimitResult.Denied(limit, retryAfter, resetAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {Key}", key);
            // Fail open - allow request if rate limiter fails
            return RateLimitResult.Allowed(policy.Limit, policy.Limit, DateTime.UtcNow.Add(policy.Window));
        }
    }

    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var pattern = $"ratelimit:*:{key}";
        
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToArray();
        
        if (keys.Length > 0)
        {
            await db.KeyDeleteAsync(keys);
            _logger.LogInformation("Reset rate limit for {Key}", key);
        }
    }

    public async Task<RateLimitState?> GetStateAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var fullKey = $"ratelimit:*:{key}";
        
        // Simplified - would need to track policy for full implementation
        var ttl = await db.KeyTimeToLiveAsync(fullKey);
        if (!ttl.HasValue) return null;

        return new RateLimitState
        {
            Key = key,
            WindowEnd = DateTime.UtcNow.Add(ttl.Value)
        };
    }

    private TimeSpan CalculateRetryAfter(RateLimitPolicy policy)
    {
        return policy.Algorithm switch
        {
            RateLimitAlgorithm.FixedWindow => policy.Window,
            RateLimitAlgorithm.SlidingWindow => TimeSpan.FromSeconds(policy.Window.TotalSeconds / policy.Limit),
            RateLimitAlgorithm.TokenBucket => TimeSpan.FromSeconds(1.0 / (policy.Limit / policy.Window.TotalSeconds)),
            _ => policy.Window
        };
    }
}
