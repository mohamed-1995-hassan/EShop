using EShop.Domain.Aggregates.ReturnAggregate;

namespace EShop.Domain.Repositories;

public interface IReturnRepository
{
    Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReturnRequest?> GetByReturnNumberAsync(string returnNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetByStatusAsync(ReturnStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetAwaitingInspectionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetInTransitAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);
    
    // Analytics
    Task<int> GetReturnCountByReasonAsync(ReturnReason reason, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRefundsIssuedAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
