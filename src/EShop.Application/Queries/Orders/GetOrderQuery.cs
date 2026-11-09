using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Orders;

// ============================================
// GET ORDER BY ID
// ============================================
public record GetOrderQuery(Guid OrderId) : IRequest<OrderDetailsDto?>;

public record OrderDetailsDto(
    Guid Id,
    Guid CustomerId,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    string Currency,
    AddressDto ShippingAddress,
    List<OrderLineDto> Items);

public record OrderLineDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDetailsDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDetailsDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null) return null;

        return MapToDto(order);
    }

    private static OrderDetailsDto MapToDto(Order order)
    {
        return new OrderDetailsDto(
            order.Id,
            order.CustomerId,
            order.OrderDate,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            new AddressDto(
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.Country,
                order.ShippingAddress.PostalCode),
            order.OrderLines.Select(ol => new OrderLineDto(
                ol.ProductId,
                ol.ProductName,
                ol.UnitPrice.Amount,
                ol.Quantity,
                ol.LineTotal.Amount)).ToList());
    }
}

// ============================================
// GET CUSTOMER ORDERS
// ============================================
public record GetCustomerOrdersQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<OrderSummaryDto>>;

public record OrderSummaryDto(
    Guid Id,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    int ItemCount);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetCustomerOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<PagedResult<OrderSummaryDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        
        var totalCount = orders.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var pagedOrders = orders
            .OrderByDescending(o => o.OrderDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderDate,
                o.Status.ToString(),
                o.TotalAmount.Amount,
                o.OrderLines.Count))
            .ToList();

        return new PagedResult<OrderSummaryDto>(
            pagedOrders,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages);
    }
}

public record AddressDto(string Street, string City, string Country, string PostalCode);
