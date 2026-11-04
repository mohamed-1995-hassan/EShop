namespace EShop.Application.DTOs;

// Response DTOs

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    DateTime OrderDate,
    string Status,
    string ShippingAddress,
    decimal TotalAmount,
    string Currency,
    List<OrderLineDto> OrderLines
);

public record OrderLineDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);

// Request DTOs

public record CreateOrderDto(
    Guid CustomerId,
    string Street,
    string City,
    string Country,
    string PostalCode
);

public record AddOrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? Currency = "KWD"
);

public record ShipOrderDto(
    string? TrackingNumber,
    string? Carrier
);

public record CancelOrderDto(
    string Reason
);
