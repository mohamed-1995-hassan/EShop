using System.Text.Json;
using EShop.Domain.Events;
using EShop.Infrastructure.CQRS.ReadModels;
using EShop.Infrastructure.EventSourcing;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.CQRS.Projections;

/// <summary>
/// Order Projection - Builds OrderReadModel from domain events
/// </summary>
public class OrderProjection : ProjectionBase
{
    private readonly IOrderReadModelRepository _repository;
    private readonly ILogger<OrderProjection> _logger;

    public override string Name => "OrderProjection";

    public OrderProjection(
        IProjectionCheckpointStore checkpointStore,
        IOrderReadModelRepository repository,
        ILogger<OrderProjection> logger) : base(checkpointStore)
    {
        _repository = repository;
        _logger = logger;

        // Register handled event types
        _handledEventTypes.Add(nameof(OrderPlacedEvent));
        _handledEventTypes.Add(nameof(OrderConfirmedEvent));
        _handledEventTypes.Add(nameof(OrderShippedEvent));
        _handledEventTypes.Add(nameof(OrderCancelledEvent));
        _handledEventTypes.Add(nameof(OrderItemAddedEvent));
    }

    public override async Task ProjectAsync(StoredEvent evt, CancellationToken cancellationToken = default)
    {
        switch (evt.EventType)
        {
            case nameof(OrderPlacedEvent):
                await HandleOrderPlaced(evt, cancellationToken);
                break;
            case nameof(OrderConfirmedEvent):
                await HandleOrderConfirmed(evt, cancellationToken);
                break;
            case nameof(OrderShippedEvent):
                await HandleOrderShipped(evt, cancellationToken);
                break;
            case nameof(OrderCancelledEvent):
                await HandleOrderCancelled(evt, cancellationToken);
                break;
            case nameof(OrderItemAddedEvent):
                await HandleOrderItemAdded(evt, cancellationToken);
                break;
        }
    }

    private async Task HandleOrderPlaced(StoredEvent evt, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<OrderPlacedEventData>(evt.Data);
        if (data == null) return;

        var orderId = Guid.Parse(evt.StreamId.Replace("order-", ""));

        var readModel = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? new OrderReadModel { Id = orderId };

        readModel.OrderNumber = $"ORD-{orderId.ToString()[..8].ToUpper()}";
        readModel.CustomerId = data.CustomerId;
        readModel.Status = "Placed";
        readModel.TotalAmount = data.TotalAmount;
        readModel.Currency = data.Currency;
        readModel.ItemCount = data.Items?.Count ?? 0;
        readModel.PlacedAt = evt.Timestamp;
        readModel.LastUpdatedAt = evt.Timestamp;

        if (data.Items != null)
        {
            readModel.Items = data.Items.Select(i => new OrderItemReadModel
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.Quantity * i.UnitPrice
            }).ToList();
        }

        await _repository.UpsertAsync(readModel, cancellationToken);
        _logger.LogDebug("Projected OrderPlaced for {OrderId}", orderId);
    }

    private async Task HandleOrderConfirmed(StoredEvent evt, CancellationToken cancellationToken)
    {
        var orderId = Guid.Parse(evt.StreamId.Replace("order-", ""));
        var readModel = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (readModel == null) return;

        readModel.Status = "Confirmed";
        readModel.ConfirmedAt = evt.Timestamp;
        readModel.LastUpdatedAt = evt.Timestamp;

        await _repository.UpsertAsync(readModel, cancellationToken);
    }

    private async Task HandleOrderShipped(StoredEvent evt, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<OrderShippedEventData>(evt.Data);
        var orderId = Guid.Parse(evt.StreamId.Replace("order-", ""));
        var readModel = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (readModel == null) return;

        readModel.Status = "Shipped";
        readModel.TrackingNumber = data?.TrackingNumber;
        readModel.Carrier = data?.Carrier;
        readModel.ShippedAt = evt.Timestamp;
        readModel.LastUpdatedAt = evt.Timestamp;

        await _repository.UpsertAsync(readModel, cancellationToken);
    }

    private async Task HandleOrderCancelled(StoredEvent evt, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<OrderCancelledEventData>(evt.Data);
        var orderId = Guid.Parse(evt.StreamId.Replace("order-", ""));
        var readModel = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (readModel == null) return;

        readModel.Status = "Cancelled";
        readModel.CancellationReason = data?.Reason;
        readModel.CancelledAt = evt.Timestamp;
        readModel.LastUpdatedAt = evt.Timestamp;

        await _repository.UpsertAsync(readModel, cancellationToken);
    }

    private async Task HandleOrderItemAdded(StoredEvent evt, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<OrderItemAddedEventData>(evt.Data);
        if (data == null) return;

        var orderId = Guid.Parse(evt.StreamId.Replace("order-", ""));
        var readModel = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? new OrderReadModel { Id = orderId, CreatedAt = evt.Timestamp };

        readModel.Items.Add(new OrderItemReadModel
        {
            ProductId = data.ProductId,
            ProductName = data.ProductName,
            Quantity = data.Quantity,
            UnitPrice = data.UnitPrice,
            LineTotal = data.Quantity * data.UnitPrice
        });

        readModel.ItemCount = readModel.Items.Count;
        readModel.TotalAmount = readModel.Items.Sum(i => i.LineTotal);
        readModel.LastUpdatedAt = evt.Timestamp;

        await _repository.UpsertAsync(readModel, cancellationToken);
    }

    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAllAsync(cancellationToken);
        await base.ResetAsync(cancellationToken);
    }

    // Event data DTOs for deserialization
    private record OrderPlacedEventData(
        Guid OrderId, 
        Guid CustomerId, 
        decimal TotalAmount, 
        string Currency,
        List<OrderItemData>? Items);

    private record OrderItemData(
        Guid ProductId, 
        string ProductName, 
        int Quantity, 
        decimal UnitPrice);

    private record OrderShippedEventData(
        string? TrackingNumber, 
        string? Carrier);

    private record OrderCancelledEventData(string? Reason);

    private record OrderItemAddedEventData(
        Guid ProductId, 
        string ProductName, 
        int Quantity, 
        decimal UnitPrice);
}

/// <summary>
/// Repository for OrderReadModel
/// </summary>
public interface IOrderReadModelRepository
{
    Task<OrderReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderReadModel>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryReadModel>> GetSummariesAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<OrderDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(OrderReadModel model, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
