using EShop.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EShop.Infrastructure.Health;

/// <summary>
/// Health check for RabbitMQ connectivity.
/// Used by Kubernetes, load balancers, and monitoring systems.
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqSettings _settings;

    public RabbitMqHealthCheck(IOptions<RabbitMqSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                VirtualHost = _settings.VirtualHost,
                UserName = _settings.Username,
                Password = _settings.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            return await Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection successful"));
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ connection failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["Host"] = _settings.Host,
                    ["Port"] = _settings.Port,
                    ["VirtualHost"] = _settings.VirtualHost
                });
        }
    }
}

/// <summary>
/// Health check for outbox processing.
/// Alerts if messages are stuck in outbox.
/// </summary>
public class OutboxHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _maxAge = TimeSpan.FromMinutes(10);
    private readonly int _maxPendingMessages = 100;

    public OutboxHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.EShopDbContext>();

            var cutoff = DateTime.UtcNow.Subtract(_maxAge);
            
            // Check for old unprocessed messages
            var stuckCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .CountAsync(dbContext.OutboxMessages
                    .Where(m => m.ProcessedOnUtc == null && m.OccurredOnUtc < cutoff), cancellationToken);

            var totalPending = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .CountAsync(dbContext.OutboxMessages
                    .Where(m => m.ProcessedOnUtc == null), cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["TotalPending"] = totalPending,
                ["StuckMessages"] = stuckCount,
                ["MaxAge"] = _maxAge.TotalMinutes
            };

            if (stuckCount > 0)
            {
                return HealthCheckResult.Degraded(
                    $"{stuckCount} messages stuck in outbox (older than {_maxAge.TotalMinutes} minutes)",
                    data: data);
            }

            if (totalPending > _maxPendingMessages)
            {
                return HealthCheckResult.Degraded(
                    $"{totalPending} messages pending in outbox (threshold: {_maxPendingMessages})",
                    data: data);
            }

            return HealthCheckResult.Healthy("Outbox processing healthy", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to check outbox health", ex);
        }
    }
}

/// <summary>
/// Health check for dead letter queue.
/// Alerts if DLQ is growing.
/// </summary>
public class DeadLetterHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _warningThreshold = 10;
    private readonly int _criticalThreshold = 50;

    public DeadLetterHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<Messaging.DeadLetter.IDeadLetterRepository>();

            if (repository == null)
            {
                return HealthCheckResult.Healthy("DLQ repository not configured");
            }

            var pendingCount = await repository.GetCountByStatusAsync(
                Messaging.DeadLetter.DeadLetterStatus.Pending, 
                cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["PendingCount"] = pendingCount,
                ["WarningThreshold"] = _warningThreshold,
                ["CriticalThreshold"] = _criticalThreshold
            };

            if (pendingCount >= _criticalThreshold)
            {
                return HealthCheckResult.Unhealthy(
                    $"Critical: {pendingCount} messages in DLQ",
                    data: data);
            }

            if (pendingCount >= _warningThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"Warning: {pendingCount} messages in DLQ",
                    data: data);
            }

            return HealthCheckResult.Healthy("Dead letter queue healthy", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to check DLQ health", ex);
        }
    }
}
