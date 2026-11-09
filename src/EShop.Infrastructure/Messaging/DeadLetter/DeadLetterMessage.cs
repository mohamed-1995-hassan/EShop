namespace EShop.Infrastructure.Messaging.DeadLetter;

/// <summary>
/// Represents a message that failed processing and was moved to the Dead Letter Queue.
/// </summary>
public class DeadLetterMessage
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Original message ID for correlation
    /// </summary>
    public Guid OriginalMessageId { get; private set; }
    
    /// <summary>
    /// Full type name of the message
    /// </summary>
    public string MessageType { get; private set; } = string.Empty;
    
    /// <summary>
    /// Serialized message content
    /// </summary>
    public string Content { get; private set; } = string.Empty;
    
    /// <summary>
    /// Original queue/endpoint the message was sent to
    /// </summary>
    public string SourceQueue { get; private set; } = string.Empty;
    
    /// <summary>
    /// Consumer that failed to process the message
    /// </summary>
    public string FailedConsumer { get; private set; } = string.Empty;
    
    /// <summary>
    /// Error message from the last failure
    /// </summary>
    public string? ErrorMessage { get; private set; }
    
    /// <summary>
    /// Full exception stack trace
    /// </summary>
    public string? StackTrace { get; private set; }
    
    /// <summary>
    /// When the message was originally created
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }
    
    /// <summary>
    /// When the message was moved to DLQ
    /// </summary>
    public DateTime FailedAtUtc { get; private set; }
    
    /// <summary>
    /// Number of retry attempts in DLQ
    /// </summary>
    public int RetryCount { get; private set; }
    
    /// <summary>
    /// When the last retry was attempted
    /// </summary>
    public DateTime? LastRetryAtUtc { get; private set; }
    
    /// <summary>
    /// Status of DLQ processing
    /// </summary>
    public DeadLetterStatus Status { get; private set; }
    
    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public Guid? CorrelationId { get; private set; }
    
    /// <summary>
    /// Conversation ID for saga tracking
    /// </summary>
    public Guid? ConversationId { get; private set; }
    
    /// <summary>
    /// Notes from manual review
    /// </summary>
    public string? Notes { get; private set; }

    private DeadLetterMessage() { } // For EF Core

    public static DeadLetterMessage Create(
        Guid originalMessageId,
        string messageType,
        string content,
        string sourceQueue,
        string failedConsumer,
        string? errorMessage,
        string? stackTrace,
        Guid? correlationId = null,
        Guid? conversationId = null)
    {
        return new DeadLetterMessage
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = originalMessageId,
            MessageType = messageType,
            Content = content,
            SourceQueue = sourceQueue,
            FailedConsumer = failedConsumer,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace,
            CreatedAtUtc = DateTime.UtcNow,
            FailedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            Status = DeadLetterStatus.Pending,
            CorrelationId = correlationId,
            ConversationId = conversationId
        };
    }

    public void MarkAsRetrying()
    {
        Status = DeadLetterStatus.Retrying;
        RetryCount++;
        LastRetryAtUtc = DateTime.UtcNow;
    }

    public void MarkAsResolved(string? notes = null)
    {
        Status = DeadLetterStatus.Resolved;
        Notes = notes;
    }

    public void MarkAsArchived(string? notes = null)
    {
        Status = DeadLetterStatus.Archived;
        Notes = notes;
    }

    public void AddNote(string note)
    {
        Notes = string.IsNullOrEmpty(Notes) 
            ? note 
            : $"{Notes}\n---\n{note}";
    }
}

public enum DeadLetterStatus
{
    Pending,      // Waiting for processing
    Retrying,     // Currently being retried
    Resolved,     // Successfully reprocessed or compensated
    Archived,     // Moved to cold storage
    ManualReview  // Flagged for human review
}
