using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Orders;

public record AddOrderItemCommand(
    Guid OrderId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity) : IRequest<ModifyOrderResult>;

public record ModifyOrderResult(
    bool Success,
    decimal? NewTotal,
    string? ErrorMessage);

public class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, ModifyOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public AddOrderItemCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<ModifyOrderResult> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
                return new ModifyOrderResult(false, null, "Order not found");

            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
                return new ModifyOrderResult(false, null, "Product not found");

            if (!product.CanReserve(request.Quantity))
                return new ModifyOrderResult(false, null, "Insufficient stock");

            var unitPrice = new Money(request.UnitPrice, "KWD");
            order.AddItem(request.ProductId, request.ProductName, unitPrice, request.Quantity);
            
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return new ModifyOrderResult(true, order.TotalAmount.Amount, null);
        }
        catch (Exception ex)
        {
            return new ModifyOrderResult(false, null, ex.Message);
        }
    }
}
