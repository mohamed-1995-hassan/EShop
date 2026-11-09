namespace EShop.Infrastructure.Messaging.Inbox;

/// <summary>
/// Inbox Message for idempotent message processing.
/// 
/// INBOX PATTERN:
/// 1. When a message arrives, check if MessageId exists in inbox
/// 2. If exists, message was already processed - skip (idempotent)
/// 3. If not exists, process message and record in inbox (same transaction)
/// 4. Guarantees exactly-once processing semantics
/// 
/// This solves duplicate message delivery (at-least-once -> exactly-once).
/// </summary>
public class InboxMessage
{
    /// <summary>
    /// Unique message ID from the message broker (e.g., RabbitMQ MessageId)
    /// </summary>
    public Guid MessageId { get; private set; }
    
    /// <summary>
    /// Full type name of the message for debugging/auditing
    /// </summary>
    public string MessageType { get; private set; } = string.Empty;
    
    /// <summary>
    /// Consumer that processed this message
    /// </summary>
    public string Consumer { get; private set; } = string.Empty;
    
    /// <summary>
    /// When the message was received and processed
    /// </summary>
    public DateTime ProcessedOnUtc { get; private set; }
    
    /// <summary>
    /// Optional: correlation ID for distributed tracing
    /// </summary>
    public Guid? CorrelationId { get; private set; }
    
    /// <summary>
    /// Optional: conversation ID for saga tracking
    /// </summary>
    public Guid? ConversationId { get; private set; }

    private InboxMessage() { } // For EF Core

    public static InboxMessage Create(
        Guid messageId, 
        string messageType, 
        string consumer,
        Guid? correlationId = null,
        Guid? conversationId = null)
    {
        return new InboxMessage
        {
            MessageId = messageId,
            MessageType = messageType,
            Consumer = consumer,
            ProcessedOnUtc = DateTime.UtcNow,
            CorrelationId = correlationId,
            ConversationId = conversationId
        };
    }
}
