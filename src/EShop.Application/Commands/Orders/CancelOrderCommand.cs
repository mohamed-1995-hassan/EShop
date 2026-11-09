using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Orders;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<CancelOrderResult>;

public record CancelOrderResult(bool Success, string? ErrorMessage);

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, CancelOrderResult>
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<CancelOrderResult> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
                return new CancelOrderResult(false, "Order not found");

            order.Cancel(request.Reason);
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return new CancelOrderResult(true, null);
        }
        catch (Exception ex)
        {
            return new CancelOrderResult(false, ex.Message);
        }
    }
}
