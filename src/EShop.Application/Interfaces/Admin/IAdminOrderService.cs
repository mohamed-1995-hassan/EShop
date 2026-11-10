using EShop.Application.DTOs;

namespace EShop.Application.Interfaces.Admin;

/// <summary>
/// Service interface for admin order management operations
/// </summary>
public interface IAdminOrderService
{
    Task<PagedResult<AdminOrderDto>> GetOrdersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? status = null,
        string? paymentStatus = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? customerId = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default);
    
    Task<AdminOrderDetailDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    
    Task<AdminOrderDetailDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    
    Task<AdminOrderDto> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    
    Task<AdminOrderDto> UpdateShippingAsync(Guid orderId, UpdateShippingRequest request, CancellationToken cancellationToken = default);
    
    Task<AdminOrderDto> AddOrderNoteAsync(Guid orderId, AddOrderNoteRequest request, CancellationToken cancellationToken = default);
    
    Task<AdminOrderDto> ProcessRefundAsync(Guid orderId, ProcessRefundRequest request, CancellationToken cancellationToken = default);
    
    Task CancelOrderAsync(Guid orderId, string? reason = null, CancellationToken cancellationToken = default);
    
    Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkUpdateStatusAsync(IReadOnlyList<Guid> orderIds, string status, CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportOrdersAsync(
        string format,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
