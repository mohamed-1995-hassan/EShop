using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Scheduling;

/// <summary>
/// Background service that processes scheduled retries
/// </summary>
public class RetrySchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetrySchedulerService> _logger;
    private readonly Dictionary<string, Func<string, Task<bool>>> _handlers = new();
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

    public RetrySchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<RetrySchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Register a retry handler for an operation type
    /// </summary>
    public void RegisterHandler(string operationType, Func<string, Task<bool>> handler)
    {
        _handlers[operationType] = handler;
        _logger.LogInformation("Registered retry handler for {OperationType}", operationType);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retry Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRetriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled retries");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Retry Scheduler Service stopped");
    }

    private async Task ProcessPendingRetriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IScheduledRetryRepository>();

        var dueRetries = await repository.GetDueRetriesAsync(DateTime.UtcNow, 50, cancellationToken);
        
        if (dueRetries.Count == 0) return;

        _logger.LogDebug("Processing {Count} scheduled retries", dueRetries.Count);

        foreach (var retry in dueRetries)
        {
            await ProcessRetryAsync(retry, repository, cancellationToken);
        }
    }

    private async Task ProcessRetryAsync(
        ScheduledRetry retry,
        IScheduledRetryRepository repository,
        CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(retry.OperationType, out var handler))
        {
            _logger.LogWarning("No handler registered for operation type {Type}", retry.OperationType);
            return;
        }

        try
        {
            // Mark as processing
            await repository.MarkProcessingAsync(retry.Id, cancellationToken);

            // Execute handler
            var success = await handler(retry.PayloadJson);

            if (success)
            {
                await repository.MarkCompletedAsync(retry.Id, cancellationToken);
                _logger.LogInformation("Retry {Id} completed successfully", retry.Id);
            }
            else
            {
                await HandleFailure(retry, "Handler returned false", repository, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing retry {Id}", retry.Id);
            await HandleFailure(retry, ex.Message, repository, cancellationToken);
        }
    }

    private async Task HandleFailure(
        ScheduledRetry retry,
        string error,
        IScheduledRetryRepository repository,
        CancellationToken cancellationToken)
    {
        var newAttemptCount = retry.AttemptCount + 1;

        if (newAttemptCount >= retry.MaxAttempts)
        {
            await repository.MarkMaxAttemptsReachedAsync(retry.Id, error, cancellationToken);
            _logger.LogWarning("Retry {Id} reached max attempts ({Max})", retry.Id, retry.MaxAttempts);
        }
        else
        {
            var policy = RetryPolicy.Default;
            var nextDelay = policy.CalculateDelay(newAttemptCount);
            var nextScheduledAt = DateTime.UtcNow.Add(nextDelay);

            await repository.RescheduleAsync(retry.Id, nextScheduledAt, newAttemptCount, error, cancellationToken);
            _logger.LogInformation(
                "Retry {Id} rescheduled for {Time} (attempt {Attempt}/{Max})",
                retry.Id, nextScheduledAt, newAttemptCount, retry.MaxAttempts);
        }
    }
}

/// <summary>
/// Repository for scheduled retries
/// </summary>
public interface IScheduledRetryRepository
{
    Task<ScheduledRetry> CreateAsync(ScheduledRetry retry, CancellationToken cancellationToken = default);
    Task<ScheduledRetry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduledRetry>> GetDueRetriesAsync(DateTime asOf, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduledRetry>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default);
    Task MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkMaxAttemptsReachedAsync(Guid id, string error, CancellationToken cancellationToken = default);
    Task RescheduleAsync(Guid id, DateTime scheduledAt, int attemptCount, string error, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation for development/testing
/// </summary>
public class InMemoryScheduledRetryRepository : IScheduledRetryRepository
{
    private readonly Dictionary<Guid, ScheduledRetry> _retries = new();
    private readonly object _lock = new();

    public Task<ScheduledRetry> CreateAsync(ScheduledRetry retry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _retries[retry.Id] = retry;
        }
        return Task.FromResult(retry);
    }

    public Task<ScheduledRetry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _retries.TryGetValue(id, out var retry);
            return Task.FromResult(retry);
        }
    }

    public Task<IReadOnlyList<ScheduledRetry>> GetDueRetriesAsync(DateTime asOf, int limit, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var due = _retries.Values
                .Where(r => r.Status == RetryStatus.Pending && r.ScheduledAt <= asOf)
                .OrderBy(r => r.ScheduledAt)
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<ScheduledRetry>>(due);
        }
    }

    public Task<IReadOnlyList<ScheduledRetry>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var retries = _retries.Values
                .Where(r => r.OperationType == operationType)
                .ToList();
            return Task.FromResult<IReadOnlyList<ScheduledRetry>>(retries);
        }
    }

    public Task MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_retries.TryGetValue(id, out var retry))
            {
                _retries[id] = retry with { Status = RetryStatus.Processing };
            }
        }
        return Task.CompletedTask;
    }

    public Task MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_retries.TryGetValue(id, out var retry))
            {
                _retries[id] = retry with { Status = RetryStatus.Completed, ExecutedAt = DateTime.UtcNow };
            }
        }
        return Task.CompletedTask;
    }

    public Task MarkMaxAttemptsReachedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_retries.TryGetValue(id, out var retry))
            {
                _retries[id] = retry with { Status = RetryStatus.MaxAttemptsReached, LastError = error };
            }
        }
        return Task.CompletedTask;
    }

    public Task RescheduleAsync(Guid id, DateTime scheduledAt, int attemptCount, string error, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_retries.TryGetValue(id, out var retry))
            {
                _retries[id] = retry with 
                { 
                    Status = RetryStatus.Pending, 
                    ScheduledAt = scheduledAt,
                    AttemptCount = attemptCount,
                    LastError = error
                };
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_retries.TryGetValue(id, out var retry) && retry.Status == RetryStatus.Pending)
            {
                _retries[id] = retry with { Status = RetryStatus.Cancelled };
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}
