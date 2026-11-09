using EShop.Application.DTOs;
using EShop.Domain.Aggregates.OrderAggregate;

namespace EShop.Application.Services;

/// <summary>
/// Application Service interface for Order operations.
/// </summary>
public interface IOrderAppService
{
    Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderDto>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task<Guid> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);
    Task AddItemAsync(Guid orderId, AddOrderItemDto dto, CancellationToken cancellationToken = default);
    Task PlaceOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task ShipOrderAsync(Guid orderId, string? trackingNumber = null, string? carrier = null, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(Guid orderId, string reason = "Customer requested", CancellationToken cancellationToken = default);
}
