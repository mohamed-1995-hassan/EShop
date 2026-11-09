using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace EShop.Infrastructure.DistributedLocking;

/// <summary>
/// Redis-based distributed lock implementation using RedLock algorithm.
/// 
/// REDLOCK ALGORITHM:
/// - Acquires locks on multiple Redis instances
/// - Provides strong consistency guarantees
/// - Handles Redis failures gracefully
/// 
/// BEST PRACTICES:
/// - Always use try-finally to release locks
/// - Set reasonable expiry times
/// - Use resource naming conventions (e.g., "order:{orderId}")
/// </summary>
public class RedisDistributedLockService : IDistributedLockService, IDisposable
{
    private readonly RedLockFactory _redLockFactory;
    private readonly ILogger<RedisDistributedLockService> _logger;
    private bool _disposed;

    public RedisDistributedLockService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisDistributedLockService> logger)
    {
        _logger = logger;
        
        var multiplexers = new List<RedLockMultiplexer>
        {
            new(connectionMultiplexer)
        };
        
        _redLockFactory = RedLockFactory.Create(multiplexers);
    }

    public async Task<IDistributedLock?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var redLock = await _redLockFactory.CreateLockAsync(
            resource,
            expiry);

        if (redLock.IsAcquired)
        {
            _logger.LogDebug("Acquired lock on {Resource} with expiry {Expiry}s", 
                resource, expiry.TotalSeconds);
            return new RedisDistributedLock(redLock, _logger);
        }

        _logger.LogDebug("Failed to acquire lock on {Resource} - already held", resource);
        await redLock.DisposeAsync();
        return null;
    }

    public async Task<IDistributedLock?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        TimeSpan wait,
        TimeSpan retry,
        CancellationToken cancellationToken = default)
    {
        var redLock = await _redLockFactory.CreateLockAsync(
            resource,
            expiry,
            wait,
            retry,
            cancellationToken);

        if (redLock.IsAcquired)
        {
            _logger.LogDebug("Acquired lock on {Resource} after waiting", resource);
            return new RedisDistributedLock(redLock, _logger);
        }

        _logger.LogWarning("Timeout acquiring lock on {Resource} after {Wait}s", 
            resource, wait.TotalSeconds);
        await redLock.DisposeAsync();
        return null;
    }

    public async Task<T> ExecuteWithLockAsync<T>(
        string resource,
        TimeSpan expiry,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await AcquireAsync(
            resource,
            expiry,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMilliseconds(100),
            cancellationToken);

        if (lockHandle == null)
        {
            throw new DistributedLockException(resource, TimeSpan.FromSeconds(30));
        }

        return await action();
    }

    public async Task ExecuteWithLockAsync(
        string resource,
        TimeSpan expiry,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithLockAsync(resource, expiry, async () =>
        {
            await action();
            return true;
        }, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _redLockFactory.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Lock handle wrapper
/// </summary>
internal class RedisDistributedLock : IDistributedLock
{
    private readonly IRedLock _redLock;
    private readonly ILogger _logger;
    private bool _disposed;

    public RedisDistributedLock(IRedLock redLock, ILogger logger)
    {
        _redLock = redLock;
        _logger = logger;
    }

    public string Resource => _redLock.Resource;
    public string LockId => _redLock.LockId;
    public bool IsAcquired => _redLock.IsAcquired;
    public DateTime? ExpiresAt => null; // RedLock doesn't expose expiry directly

    public Task<bool> ExtendAsync(TimeSpan extension)
    {
        // RedLock.net doesn't support extension in current version
        // Would need to re-acquire with longer timeout
        _logger.LogWarning("Lock extension not supported in current RedLock version");
        return Task.FromResult(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _redLock.Dispose();
        _logger.LogDebug("Released lock on {Resource}", Resource);
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _redLock.DisposeAsync();
        _logger.LogDebug("Released lock on {Resource}", Resource);
        _disposed = true;
    }
}
