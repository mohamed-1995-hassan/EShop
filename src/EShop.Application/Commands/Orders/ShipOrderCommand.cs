using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Orders;

public record ShipOrderCommand(
    Guid OrderId,
    string? TrackingNumber = null,
    string? Carrier = null) : IRequest<ShipOrderResult>;

public record ShipOrderResult(bool Success, string? TrackingNumber, string? ErrorMessage);

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, ShipOrderResult>
{
    private readonly IOrderRepository _orderRepository;

    public ShipOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ShipOrderResult> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
                return new ShipOrderResult(false, null, "Order not found");

            order.Ship(request.TrackingNumber, request.Carrier);
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return new ShipOrderResult(true, request.TrackingNumber, null);
        }
        catch (Exception ex)
        {
            return new ShipOrderResult(false, null, ex.Message);
        }
    }
}
