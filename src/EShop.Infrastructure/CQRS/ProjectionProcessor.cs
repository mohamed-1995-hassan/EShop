using EShop.Infrastructure.EventSourcing;
using EShop.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.CQRS;

/// <summary>
/// PROJECTION PROCESSOR
/// 
/// Background service that:
/// 1. Reads events from event store
/// 2. Dispatches to relevant projections
/// 3. Tracks progress (checkpoints)
/// 4. Handles failures with retry
/// </summary>
public class ProjectionProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEnumerable<IProjection> _projections;
    private readonly ILogger<ProjectionProcessor> _logger;
    private readonly MessagingMetrics _metrics;
    
    private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(100);
    private readonly int _batchSize = 100;

    public ProjectionProcessor(
        IServiceScopeFactory scopeFactory,
        IEnumerable<IProjection> projections,
        ILogger<ProjectionProcessor> logger,
        MessagingMetrics metrics)
    {
        _scopeFactory = scopeFactory;
        _projections = projections;
        _logger = logger;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Projection Processor started with {Count} projections", 
            _projections.Count());

        // Process each projection in parallel
        var tasks = _projections.Select(p => ProcessProjectionAsync(p, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessProjectionAsync(IProjection projection, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting projection {Name}", projection.Name);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(projection, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in projection {Name}, retrying in 5s", projection.Name);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task ProcessBatchAsync(IProjection projection, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        var checkpoint = await projection.GetCheckpointAsync(cancellationToken);
        var processedCount = 0;
        var lastPosition = checkpoint;

        await foreach (var evt in eventStore.ReadAllAsync(checkpoint + 1, _batchSize, cancellationToken))
        {
            // Skip events this projection doesn't handle
            if (!projection.HandledEventTypes.Contains(evt.EventType))
            {
                lastPosition = evt.GlobalPosition;
                continue;
            }

            try
            {
                await projection.ProjectAsync(evt, cancellationToken);
                lastPosition = evt.GlobalPosition;
                processedCount++;

                _metrics.RecordMessageConsumed(evt.EventType, projection.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error projecting event {EventId} in {Projection}", 
                    evt.EventId, projection.Name);
                
                _metrics.RecordMessageFailed(evt.EventType, ex.Message);
                throw;
            }
        }

        // Save checkpoint after batch
        if (lastPosition > checkpoint)
        {
            await projection.SaveCheckpointAsync(lastPosition, cancellationToken);
            
            if (processedCount > 0)
            {
                _logger.LogDebug(
                    "Projection {Name} processed {Count} events, checkpoint at {Position}",
                    projection.Name, processedCount, lastPosition);
            }
        }

        // Wait before next poll if no events
        if (processedCount == 0)
        {
            await Task.Delay(_pollInterval, cancellationToken);
        }
    }
}

/// <summary>
/// Projection manager for administration
/// </summary>
public interface IProjectionManager
{
    Task<IReadOnlyList<ProjectionStatus>> GetStatusAsync(CancellationToken cancellationToken = default);
    Task RebuildAsync(string projectionName, CancellationToken cancellationToken = default);
    Task RebuildAllAsync(CancellationToken cancellationToken = default);
    Task PauseAsync(string projectionName, CancellationToken cancellationToken = default);
    Task ResumeAsync(string projectionName, CancellationToken cancellationToken = default);
}

public class ProjectionStatus
{
    public string Name { get; init; } = string.Empty;
    public long Checkpoint { get; init; }
    public long EventStorePosition { get; init; }
    public long Lag => EventStorePosition - Checkpoint;
    public bool IsRunning { get; init; }
    public DateTime? LastUpdated { get; init; }
}
