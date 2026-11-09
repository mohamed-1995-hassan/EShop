using System.Text.Json;
using EShop.Domain.Common;

namespace EShop.Infrastructure.Persistence.Outbox;

/// <summary>
/// Outbox Message for reliable event publishing.
/// 
/// OUTBOX PATTERN:
/// 1. Domain events are converted to OutboxMessage and saved in SAME transaction as entity changes
/// 2. Background worker reads unprocessed messages and publishes them
/// 3. If publishing fails, message remains for retry
/// 4. Guarantees at-least-once delivery
/// 
/// This solves the "dual write" problem - either both the entity AND the event are persisted, or neither.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage() { } // For EF Core

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredOnUtc = domainEvent.OccurredOn,
            ProcessedOnUtc = null,
            Error = null,
            RetryCount = 0
        };
    }

    public IDomainEvent? DeserializeEvent()
    {
        var type = System.Type.GetType(Type);
        if (type == null) return null;

        return JsonSerializer.Deserialize(Content, type) as IDomainEvent;
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
