namespace EShop.Infrastructure.EventSourcing;

/// <summary>
/// EVENT STORE INTERFACE
/// 
/// Event Sourcing stores the full history of state changes as events.
/// 
/// BENEFITS:
/// - Complete audit trail
/// - Temporal queries (state at any point in time)
/// - Event replay for debugging
/// - Easy integration with projections (CQRS)
/// 
/// TRADE-OFFS:
/// - Increased storage
/// - More complex queries
/// - Schema evolution challenges
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Append events to a stream
    /// </summary>
    /// <param name="streamId">Aggregate/stream identifier</param>
    /// <param name="events">Events to append</param>
    /// <param name="expectedVersion">Expected stream version for optimistic concurrency</param>
    Task<long> AppendToStreamAsync(
        string streamId,
        IEnumerable<StoredEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read all events from a stream
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read events from a stream in batches
    /// </summary>
    IAsyncEnumerable<StoredEvent> ReadStreamAsyncEnumerable(
        string streamId,
        long fromVersion = 0,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read all events (for projections)
    /// </summary>
    IAsyncEnumerable<StoredEvent> ReadAllAsync(
        long fromPosition = 0,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current version of a stream
    /// </summary>
    Task<long> GetStreamVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if stream exists
    /// </summary>
    Task<bool> StreamExistsAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to all events (for real-time projections)
    /// </summary>
    Task SubscribeToAllAsync(
        Func<StoredEvent, Task> handler,
        long? fromPosition = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Event as stored in the event store
/// </summary>
public class StoredEvent
{
    /// <summary>
    /// Unique event ID
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Stream/aggregate ID
    /// </summary>
    public string StreamId { get; init; } = string.Empty;

    /// <summary>
    /// Event type name (for deserialization)
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Serialized event data (JSON)
    /// </summary>
    public string Data { get; init; } = string.Empty;

    /// <summary>
    /// Event metadata (correlation, causation, user)
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Version within the stream
    /// </summary>
    public long StreamVersion { get; init; }

    /// <summary>
    /// Global position across all streams
    /// </summary>
    public long GlobalPosition { get; init; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event metadata for tracing and auditing
/// </summary>
public class EventMetadata
{
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Source { get; init; }
    public Dictionary<string, string>? CustomProperties { get; init; }
}

/// <summary>
/// Stream version constants
/// </summary>
public static class StreamVersion
{
    public const long NoStream = -1;
    public const long Any = -2;
    public const long StreamExists = -4;
}

/// <summary>
/// Exception for optimistic concurrency conflicts
/// </summary>
public class ConcurrencyException : Exception
{
    public string StreamId { get; }
    public long ExpectedVersion { get; }
    public long ActualVersion { get; }

    public ConcurrencyException(string streamId, long expectedVersion, long actualVersion)
        : base($"Concurrency conflict on stream '{streamId}'. Expected version {expectedVersion}, but found {actualVersion}.")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
