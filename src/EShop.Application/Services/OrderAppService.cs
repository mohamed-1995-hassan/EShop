using EShop.Application.DTOs;
using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Common;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EShop.Application.Services;

/// <summary>
/// Application Service for Order operations.
/// 
/// RESPONSIBILITIES:
/// 1. Coordinate use cases (create order, place order, etc.)
/// 2. Load aggregates from repository
/// 3. Call domain methods
/// 4. Save changes
/// 5. Dispatch domain events
/// 
/// NOTE: Business logic is in the Domain (Order aggregate),
/// NOT here in the Application Service.
/// </summary>
public class OrderAppService : IOrderAppService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<OrderAppService> _logger;
    
    public OrderAppService(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<OrderAppService> logger)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        return order == null ? null : MapToDto(order);
    }
    
    public async Task<IEnumerable<OrderDto>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return orders.Select(MapToDto);
    }
    
    public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByStatusAsync(status, cancellationToken);
        return orders.Select(MapToDto);
    }
    
    /// <summary>
    /// Create a new order
    /// </summary>
    public async Task<Guid> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new order for Customer {CustomerId}", dto.CustomerId);
        
        // Create value object
        var address = new Address(dto.Street, dto.City, dto.Country, dto.PostalCode);
        
        // Use factory method on aggregate
        var order = Order.Create(dto.CustomerId, address);
        
        // Persist
        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        
        return order.Id;
    }
    
    /// <summary>
    /// Add item to an order
    /// </summary>
    public async Task AddItemAsync(Guid orderId, AddOrderItemDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding item to Order {OrderId}", orderId);
        
        // Load aggregate
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found");
        
        // Create value object
        var unitPrice = new Money(dto.UnitPrice, dto.Currency ?? "KWD");
        
        // Call behavior on aggregate (business rules enforced here)
        order.AddItem(dto.ProductId, dto.ProductName, unitPrice, dto.Quantity);
        
        // Persist changes and dispatch events
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Item added to Order {OrderId}", orderId);
    }
    
    /// <summary>
    /// Place an order - KEY USE CASE
    /// This triggers multiple domain events and handlers
    /// </summary>
    public async Task PlaceOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════");
        _logger.LogInformation("🛒 PLACING ORDER {OrderId}", orderId);
        _logger.LogInformation("═══════════════════════════════════════════════════");
        
        // 1. Load aggregate
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found");
        
        // 2. Execute business operation (domain logic, raises events)
        order.Place();
        
        // 3. Persist changes with outbox (events saved atomically)
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesWithOutboxAsync(cancellationToken);
        
        _logger.LogInformation("═══════════════════════════════════════════════════");
        _logger.LogInformation("✅ ORDER {OrderId} PLACED SUCCESSFULLY", orderId);
        _logger.LogInformation("═══════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// Ship an order
    /// </summary>
    public async Task ShipOrderAsync(Guid orderId, string? trackingNumber = null, string? carrier = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚚 Shipping Order {OrderId}", orderId);
        
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found");
        
        order.Ship(trackingNumber, carrier);
        
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesWithOutboxAsync(cancellationToken);
        
        _logger.LogInformation("✅ Order {OrderId} shipped", orderId);
    }
    
    /// <summary>
    /// Cancel an order
    /// </summary>
    public async Task CancelOrderAsync(Guid orderId, string reason = "Customer requested", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("❌ Cancelling Order {OrderId}", orderId);
        
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found");
        
        order.Cancel(reason);
        
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesWithOutboxAsync(cancellationToken);
        
        _logger.LogInformation("✅ Order {OrderId} cancelled", orderId);
    }
    
    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.OrderDate,
            order.Status.ToString(),
            order.ShippingAddress.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.OrderLines.Select(line => new OrderLineDto(
                line.ProductId,
                line.ProductName,
                line.UnitPrice.Amount,
                line.Quantity,
                line.LineTotal.Amount
            )).ToList()
        );
    }
}
