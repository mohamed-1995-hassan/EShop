using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.EventSourcing;

/// <summary>
/// PostgreSQL-based event store implementation.
/// 
/// Uses a single events table with:
/// - Stream partitioning by stream_id
/// - Global ordering by position
/// - Optimistic concurrency via stream version
/// </summary>
public class PostgresEventStore : IEventStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostgresEventStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostgresEventStore(
        IServiceScopeFactory scopeFactory,
        ILogger<PostgresEventStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<long> AppendToStreamAsync(
        string streamId,
        IEnumerable<StoredEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check current version with row lock
            var currentVersion = await dbContext.Events
                .Where(e => e.StreamId == streamId)
                .MaxAsync(e => (long?)e.StreamVersion, cancellationToken) ?? StreamVersion.NoStream;

            if (expectedVersion != StreamVersion.Any && currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(streamId, expectedVersion, currentVersion);
            }

            var newVersion = currentVersion;
            var eventsToAdd = events.Select(e =>
            {
                newVersion++;
                return new EventEntity
                {
                    EventId = e.EventId,
                    StreamId = streamId,
                    EventType = e.EventType,
                    Data = e.Data,
                    Metadata = e.Metadata,
                    StreamVersion = newVersion,
                    Timestamp = e.Timestamp
                };
            }).ToList();

            await dbContext.Events.AddRangeAsync(eventsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Appended {Count} events to stream {StreamId}, new version {Version}",
                eventsToAdd.Count, streamId, newVersion);

            return newVersion;
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error appending events to stream {StreamId}", streamId);
            throw;
        }
    }

    public async Task<IReadOnlyList<StoredEvent>> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        var events = await dbContext.Events
            .Where(e => e.StreamId == streamId && e.StreamVersion >= fromVersion)
            .OrderBy(e => e.StreamVersion)
            .Select(e => new StoredEvent
            {
                EventId = e.EventId,
                StreamId = e.StreamId,
                EventType = e.EventType,
                Data = e.Data,
                Metadata = e.Metadata,
                StreamVersion = e.StreamVersion,
                GlobalPosition = e.GlobalPosition,
                Timestamp = e.Timestamp
            })
            .ToListAsync(cancellationToken);

        return events;
    }

    public async IAsyncEnumerable<StoredEvent> ReadStreamAsyncEnumerable(
        string streamId,
        long fromVersion = 0,
        int batchSize = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        var currentVersion = fromVersion;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await dbContext.Events
                .Where(e => e.StreamId == streamId && e.StreamVersion >= currentVersion)
                .OrderBy(e => e.StreamVersion)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0) yield break;

            foreach (var e in batch)
            {
                yield return new StoredEvent
                {
                    EventId = e.EventId,
                    StreamId = e.StreamId,
                    EventType = e.EventType,
                    Data = e.Data,
                    Metadata = e.Metadata,
                    StreamVersion = e.StreamVersion,
                    GlobalPosition = e.GlobalPosition,
                    Timestamp = e.Timestamp
                };

                currentVersion = e.StreamVersion + 1;
            }

            if (batch.Count < batchSize) yield break;
        }
    }

    public async IAsyncEnumerable<StoredEvent> ReadAllAsync(
        long fromPosition = 0,
        int batchSize = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        var currentPosition = fromPosition;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await dbContext.Events
                .Where(e => e.GlobalPosition >= currentPosition)
                .OrderBy(e => e.GlobalPosition)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0) yield break;

            foreach (var e in batch)
            {
                yield return new StoredEvent
                {
                    EventId = e.EventId,
                    StreamId = e.StreamId,
                    EventType = e.EventType,
                    Data = e.Data,
                    Metadata = e.Metadata,
                    StreamVersion = e.StreamVersion,
                    GlobalPosition = e.GlobalPosition,
                    Timestamp = e.Timestamp
                };

                currentPosition = e.GlobalPosition + 1;
            }

            if (batch.Count < batchSize) yield break;
        }
    }

    public async Task<long> GetStreamVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        return await dbContext.Events
            .Where(e => e.StreamId == streamId)
            .MaxAsync(e => (long?)e.StreamVersion, cancellationToken) ?? StreamVersion.NoStream;
    }

    public async Task<bool> StreamExistsAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        return await dbContext.Events.AnyAsync(e => e.StreamId == streamId, cancellationToken);
    }

    public async Task SubscribeToAllAsync(
        Func<StoredEvent, Task> handler,
        long? fromPosition = null,
        CancellationToken cancellationToken = default)
    {
        var position = fromPosition ?? 0;
        var pollInterval = TimeSpan.FromMilliseconds(100);

        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var evt in ReadAllAsync(position, 100, cancellationToken))
            {
                await handler(evt);
                position = evt.GlobalPosition + 1;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }
    }
}

/// <summary>
/// EF Core entity for events table
/// </summary>
public class EventEntity
{
    public Guid EventId { get; set; }
    public string StreamId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public long StreamVersion { get; set; }
    public long GlobalPosition { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// DbContext for event store
/// </summary>
public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options) { }

    public DbSet<EventEntity> Events => Set<EventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.ToTable("event_store");
            entity.HasKey(e => e.GlobalPosition);

            entity.Property(e => e.GlobalPosition)
                .HasColumnName("global_position")
                .UseIdentityAlwaysColumn();

            entity.Property(e => e.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            entity.Property(e => e.StreamId)
                .HasColumnName("stream_id")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Data)
                .HasColumnName("data")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            entity.Property(e => e.StreamVersion)
                .HasColumnName("stream_version")
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();

            // Indexes
            entity.HasIndex(e => new { e.StreamId, e.StreamVersion })
                .IsUnique()
                .HasDatabaseName("ix_event_store_stream_version");

            entity.HasIndex(e => e.StreamId)
                .HasDatabaseName("ix_event_store_stream_id");

            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("ix_event_store_event_type");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_event_store_timestamp");
        });
    }
}
