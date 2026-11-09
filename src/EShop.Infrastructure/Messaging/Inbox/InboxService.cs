using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Messaging.Inbox;

public class InboxService : IInboxService
{
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<InboxService> _logger;

    public InboxService(EShopDbContext dbContext, ILogger<InboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InboxMessages
            .AnyAsync(m => m.MessageId == messageId, cancellationToken);
    }

    public async Task RecordProcessedAsync(
        Guid messageId,
        string messageType,
        string consumer,
        Guid? correlationId = null,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        var inboxMessage = InboxMessage.Create(
            messageId, 
            messageType, 
            consumer, 
            correlationId, 
            conversationId);

        await _dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
        // Note: SaveChanges should be called by the outer transaction
    }

    public async Task<bool> TryProcessAsync(
        Guid messageId,
        string messageType,
        string consumer,
        Func<Task> processAction,
        Guid? correlationId = null,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        // Check if already processed
        if (await IsProcessedAsync(messageId, cancellationToken))
        {
            _logger.LogInformation(
                "Message {MessageId} of type {MessageType} already processed by {Consumer}, skipping",
                messageId, messageType, consumer);
            return false;
        }

        // Process the message
        await processAction();

        // Record as processed
        await RecordProcessedAsync(
            messageId, 
            messageType, 
            consumer, 
            correlationId, 
            conversationId, 
            cancellationToken);

        // Save both the processing result and inbox record atomically
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Message {MessageId} of type {MessageType} processed successfully by {Consumer}",
            messageId, messageType, consumer);

        return true;
    }
}
