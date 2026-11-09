namespace EShop.Infrastructure.Messaging.Inbox;

/// <summary>
/// Service for inbox pattern operations.
/// Use this in message consumers to ensure idempotent processing.
/// </summary>
public interface IInboxService
{
    /// <summary>
    /// Check if a message has already been processed.
    /// </summary>
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Record a message as processed.
    /// Call this AFTER successful processing, within the same transaction.
    /// </summary>
    Task RecordProcessedAsync(
        Guid messageId, 
        string messageType, 
        string consumer,
        Guid? correlationId = null,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Try to process a message idempotently.
    /// Returns false if already processed, true if processing should proceed.
    /// </summary>
    Task<bool> TryProcessAsync(
        Guid messageId,
        string messageType,
        string consumer,
        Func<Task> processAction,
        Guid? correlationId = null,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default);
}
