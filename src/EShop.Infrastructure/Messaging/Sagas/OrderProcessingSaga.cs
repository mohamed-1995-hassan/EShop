using EShop.Infrastructure.Messaging.IntegrationEvents;
using MassTransit;

namespace EShop.Infrastructure.Messaging.Sagas;

/// <summary>
/// ORDER PROCESSING SAGA STATE MACHINE
/// 
/// Coordinates the long-running order processing workflow across multiple services.
/// 
/// SAGA FLOW:
/// ┌─────────────────┐
/// │  OrderPlaced    │
/// └────────┬────────┘
///          │
///          ▼
/// ┌─────────────────┐     ┌────────────────────┐
/// │ ReservingStock  │────►│ StockReserveFailed │──► CancelOrder
/// └────────┬────────┘     └────────────────────┘
///          │ StockReserved
///          ▼
/// ┌─────────────────┐     ┌────────────────────┐
/// │ProcessingPayment│────►│  PaymentFailed     │──► ReleaseStock ──► CancelOrder
/// └────────┬────────┘     └────────────────────┘
///          │ PaymentCompleted
///          ▼
/// ┌─────────────────┐
/// │  OrderConfirmed │
/// └─────────────────┘
/// 
/// COMPENSATING TRANSACTIONS:
/// - If stock reservation fails: Cancel order (no compensation needed)
/// - If payment fails: Release reserved stock, then cancel order
/// </summary>
public class OrderProcessingSaga : MassTransitStateMachine<OrderProcessingSagaState>
{
    // States
    public State ReservingStock { get; private set; } = null!;
    public State ProcessingPayment { get; private set; } = null!;
    public State ReleasingStock { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    // Events
    public Event<OrderPlacedIntegrationEvent> OrderPlaced { get; private set; } = null!;
    public Event<StockReservedIntegrationEvent> StockReserved { get; private set; } = null!;
    public Event<StockReservationFailedIntegrationEvent> StockReservationFailed { get; private set; } = null!;
    public Event<PaymentCompletedIntegrationEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedIntegrationEvent> PaymentFailed { get; private set; } = null!;
    public Event<StockReleasedIntegrationEvent> StockReleased { get; private set; } = null!;

    public OrderProcessingSaga()
    {
        // Define the correlation property
        InstanceState(x => x.CurrentState);

        // Event correlation - all events use OrderId
        Event(() => OrderPlaced, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReserved, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentCompleted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReleased, x => x.CorrelateById(context => context.Message.OrderId));

        // Initial state - when order is placed
        Initially(
            When(OrderPlaced)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.TotalAmount = context.Message.TotalAmount;
                    context.Saga.Currency = context.Message.Currency;
                    context.Saga.CreatedAtUtc = DateTime.UtcNow;
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                })
                .Publish(context => new ReserveStockCommand
                {
                    OrderId = context.Message.OrderId,
                    Items = context.Message.Items.Select(i => new ReserveStockItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList(),
                    CorrelationId = context.Message.CorrelationId,
                    ConversationId = context.Saga.CorrelationId
                })
                .TransitionTo(ReservingStock));

        // Reserving stock - waiting for inventory response
        During(ReservingStock,
            When(StockReserved)
                .Then(context =>
                {
                    context.Saga.StockReservedAtUtc = DateTime.UtcNow;
                })
                .Publish(context => new ProcessPaymentCommand
                {
                    OrderId = context.Saga.OrderId,
                    CustomerId = context.Saga.CustomerId,
                    Amount = context.Saga.TotalAmount,
                    Currency = context.Saga.Currency,
                    CorrelationId = context.Saga.CorrelationId,
                    ConversationId = context.Saga.CorrelationId
                })
                .TransitionTo(ProcessingPayment),

            When(StockReservationFailed)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.Reason;
                    context.Saga.FailedAtUtc = DateTime.UtcNow;
                })
                .Publish(context => new CancelOrderCommand
                {
                    OrderId = context.Saga.OrderId,
                    Reason = $"Stock reservation failed: {context.Message.Reason}",
                    CorrelationId = context.Saga.CorrelationId
                })
                .TransitionTo(Cancelled));

        // Processing payment - waiting for payment response
        During(ProcessingPayment,
            When(PaymentCompleted)
                .Then(context =>
                {
                    context.Saga.PaymentId = context.Message.PaymentId;
                    context.Saga.PaymentCompletedAtUtc = DateTime.UtcNow;
                })
                .Publish(context => new ConfirmOrderCommand
                {
                    OrderId = context.Saga.OrderId,
                    CorrelationId = context.Saga.CorrelationId
                })
                .TransitionTo(Confirmed)
                .Finalize(),

            When(PaymentFailed)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.Reason;
                    context.Saga.FailedAtUtc = DateTime.UtcNow;
                })
                // Compensating transaction: release reserved stock
                .Publish(context => new ReleaseStockCommand
                {
                    OrderId = context.Saga.OrderId,
                    CorrelationId = context.Saga.CorrelationId
                })
                .TransitionTo(ReleasingStock));

        // Releasing stock (compensation) - waiting for stock release confirmation
        During(ReleasingStock,
            When(StockReleased)
                .Publish(context => new CancelOrderCommand
                {
                    OrderId = context.Saga.OrderId,
                    Reason = $"Payment failed: {context.Saga.FailureReason}",
                    CorrelationId = context.Saga.CorrelationId
                })
                .TransitionTo(Cancelled));

        // Set completed states to remove from repository
        SetCompletedWhenFinalized();
    }
}
