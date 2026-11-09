namespace EShop.Infrastructure.Messaging.DeadLetter;

/// <summary>
/// Repository for dead letter message operations
/// </summary>
public interface IDeadLetterRepository
{
    Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<DeadLetterMessage>> GetPendingAsync(int limit, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<DeadLetterMessage>> GetByStatusAsync(DeadLetterStatus status, int limit, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<DeadLetterMessage>> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<DeadLetterMessage>> GetByMessageTypeAsync(string messageType, int limit, CancellationToken cancellationToken = default);
    
    Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
    
    Task<int> GetCountByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get statistics for monitoring dashboard
    /// </summary>
    Task<DeadLetterStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class DeadLetterStatistics
{
    public int TotalCount { get; init; }
    public int PendingCount { get; init; }
    public int RetryingCount { get; init; }
    public int ResolvedCount { get; init; }
    public int ArchivedCount { get; init; }
    public Dictionary<string, int> CountByMessageType { get; init; } = new();
    public Dictionary<string, int> CountBySourceQueue { get; init; } = new();
    public DateTime? OldestPendingDate { get; init; }
}
