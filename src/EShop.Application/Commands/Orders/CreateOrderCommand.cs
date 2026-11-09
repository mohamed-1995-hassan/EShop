using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Orders;

// ============================================
// CREATE ORDER COMMAND
// ============================================
public record CreateOrderCommand(
    Guid CustomerId,
    AddressDto ShippingAddress,
    List<OrderItemDto> Items,
    string? PromoCode = null,
    int? LoyaltyPointsToRedeem = null) : IRequest<CreateOrderResult>;

public record AddressDto(
    string Street,
    string City,
    string Country,
    string PostalCode);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

public record CreateOrderResult(
    bool Success,
    Guid? OrderId,
    string? OrderNumber,
    decimal? TotalAmount,
    string? ErrorMessage);

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate customer exists
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return new CreateOrderResult(false, null, null, null, "Customer not found");

            // Create shipping address
            var shippingAddress = new Address(
                request.ShippingAddress.Street,
                request.ShippingAddress.City,
                request.ShippingAddress.Country,
                request.ShippingAddress.PostalCode);

            // Create order
            var order = Order.Create(request.CustomerId, shippingAddress);

            // Add items
            foreach (var item in request.Items)
            {
                // Validate product exists and has stock
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product == null)
                    return new CreateOrderResult(false, null, null, null, $"Product {item.ProductId} not found");

                if (!product.CanReserve(item.Quantity))
                    return new CreateOrderResult(false, null, null, null, $"Insufficient stock for {product.Name}");

                var unitPrice = new Money(item.UnitPrice, "KWD");
                order.AddItem(item.ProductId, item.ProductName, unitPrice, item.Quantity);
            }

            // Place the order
            order.Place();

            // Save
            await _orderRepository.AddAsync(order, cancellationToken);

            return new CreateOrderResult(
                true,
                order.Id,
                $"ORD-{order.Id.ToString()[..8].ToUpper()}",
                order.TotalAmount.Amount,
                null);
        }
        catch (Exception ex)
        {
            return new CreateOrderResult(false, null, null, null, ex.Message);
        }
    }
}
