using MassTransit;

namespace EShop.Infrastructure.Messaging.Sagas;

/// <summary>
/// Saga state entity - persisted to database.
/// Tracks the progress of order processing workflow.
/// </summary>
public class OrderProcessingSagaState : SagaStateMachineInstance
{
    /// <summary>
    /// Unique correlation ID for this saga instance.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Current state of the saga (e.g., "ReservingStock", "ProcessingPayment")
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    // Order information
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "KWD";

    // Payment information
    public Guid? PaymentId { get; set; }

    // Failure tracking
    public string? FailureReason { get; set; }

    // Timestamps for tracking progress
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StockReservedAtUtc { get; set; }
    public DateTime? PaymentCompletedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }

    // Row version for optimistic concurrency (required by MassTransit)
    public byte[]? RowVersion { get; set; }
}
