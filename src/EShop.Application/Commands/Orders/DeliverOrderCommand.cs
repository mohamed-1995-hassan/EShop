using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Orders;

public record DeliverOrderCommand(Guid OrderId) : IRequest<DeliverOrderResult>;

public record DeliverOrderResult(bool Success, string? ErrorMessage);

public class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, DeliverOrderResult>
{
    private readonly IOrderRepository _orderRepository;

    public DeliverOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<DeliverOrderResult> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
                return new DeliverOrderResult(false, "Order not found");

            order.Complete();
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return new DeliverOrderResult(true, null);
        }
        catch (Exception ex)
        {
            return new DeliverOrderResult(false, ex.Message);
        }
    }
}
