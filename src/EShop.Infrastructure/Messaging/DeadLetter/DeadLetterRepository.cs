using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Messaging.DeadLetter;

public class DeadLetterRepository : IDeadLetterRepository
{
    private readonly EShopDbContext _dbContext;

    public DeadLetterRepository(EShopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DeadLetterMessage>> GetPendingAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages
            .Where(m => m.Status == DeadLetterStatus.Pending)
            .OrderBy(m => m.FailedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeadLetterMessage>> GetByStatusAsync(
        DeadLetterStatus status, 
        int limit, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.FailedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeadLetterMessage>> GetByCorrelationIdAsync(
        Guid correlationId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages
            .Where(m => m.CorrelationId == correlationId)
            .OrderBy(m => m.FailedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeadLetterMessage>> GetByMessageTypeAsync(
        string messageType, 
        int limit, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages
            .Where(m => m.MessageType.Contains(messageType))
            .OrderByDescending(m => m.FailedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.DeadLetterMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.DeadLetterMessages.Update(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCountByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages
            .CountAsync(m => m.Status == status, cancellationToken);
    }

    public async Task<DeadLetterStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statusCounts = await _dbContext.DeadLetterMessages
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var messageTypeCounts = await _dbContext.DeadLetterMessages
            .Where(m => m.Status == DeadLetterStatus.Pending)
            .GroupBy(m => m.MessageType)
            .Select(g => new { MessageType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var sourceQueueCounts = await _dbContext.DeadLetterMessages
            .Where(m => m.Status == DeadLetterStatus.Pending)
            .GroupBy(m => m.SourceQueue)
            .Select(g => new { SourceQueue = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var oldestPending = await _dbContext.DeadLetterMessages
            .Where(m => m.Status == DeadLetterStatus.Pending)
            .OrderBy(m => m.FailedAtUtc)
            .Select(m => (DateTime?)m.FailedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new DeadLetterStatistics
        {
            TotalCount = statusCounts.Sum(s => s.Count),
            PendingCount = statusCounts.FirstOrDefault(s => s.Status == DeadLetterStatus.Pending)?.Count ?? 0,
            RetryingCount = statusCounts.FirstOrDefault(s => s.Status == DeadLetterStatus.Retrying)?.Count ?? 0,
            ResolvedCount = statusCounts.FirstOrDefault(s => s.Status == DeadLetterStatus.Resolved)?.Count ?? 0,
            ArchivedCount = statusCounts.FirstOrDefault(s => s.Status == DeadLetterStatus.Archived)?.Count ?? 0,
            CountByMessageType = messageTypeCounts.ToDictionary(m => m.MessageType, m => m.Count),
            CountBySourceQueue = sourceQueueCounts.ToDictionary(s => s.SourceQueue, s => s.Count),
            OldestPendingDate = oldestPending
        };
    }
}
