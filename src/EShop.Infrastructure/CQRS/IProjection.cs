using EShop.Infrastructure.EventSourcing;

namespace EShop.Infrastructure.CQRS;

/// <summary>
/// CQRS PROJECTION INTERFACE
/// 
/// Projections build read models from events for optimized queries.
/// 
/// BENEFITS:
/// - Optimized query models (denormalized)
/// - Multiple read models per aggregate
/// - Eventually consistent (async)
/// - Can rebuild from event store
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Projection name for tracking
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Event types this projection handles
    /// </summary>
    IReadOnlySet<string> HandledEventTypes { get; }

    /// <summary>
    /// Process an event
    /// </summary>
    Task ProjectAsync(StoredEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the last processed position
    /// </summary>
    Task<long> GetCheckpointAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the checkpoint after processing
    /// </summary>
    Task SaveCheckpointAsync(long position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset the projection (for rebuild)
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for projections with common functionality
/// </summary>
public abstract class ProjectionBase : IProjection
{
    private readonly IProjectionCheckpointStore _checkpointStore;
    protected readonly HashSet<string> _handledEventTypes = new();

    public abstract string Name { get; }
    public IReadOnlySet<string> HandledEventTypes => _handledEventTypes;

    protected ProjectionBase(IProjectionCheckpointStore checkpointStore)
    {
        _checkpointStore = checkpointStore;
    }

    public abstract Task ProjectAsync(StoredEvent evt, CancellationToken cancellationToken = default);

    public Task<long> GetCheckpointAsync(CancellationToken cancellationToken = default)
        => _checkpointStore.GetCheckpointAsync(Name, cancellationToken);

    public Task SaveCheckpointAsync(long position, CancellationToken cancellationToken = default)
        => _checkpointStore.SaveCheckpointAsync(Name, position, cancellationToken);

    public virtual Task ResetAsync(CancellationToken cancellationToken = default)
        => _checkpointStore.ResetCheckpointAsync(Name, cancellationToken);
}

/// <summary>
/// Checkpoint store for tracking projection progress
/// </summary>
public interface IProjectionCheckpointStore
{
    Task<long> GetCheckpointAsync(string projectionName, CancellationToken cancellationToken = default);
    Task SaveCheckpointAsync(string projectionName, long position, CancellationToken cancellationToken = default);
    Task ResetCheckpointAsync(string projectionName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read model query interface
/// </summary>
public interface IReadModelQuery<TModel> where TModel : class
{
    Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TModel>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
}
