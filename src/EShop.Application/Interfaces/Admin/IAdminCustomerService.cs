using EShop.Application.DTOs;

namespace EShop.Application.Interfaces.Admin;

/// <summary>
/// Service interface for admin customer management operations
/// </summary>
public interface IAdminCustomerService
{
    Task<PagedResult<AdminCustomerDto>> GetCustomersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null,
        string? tier = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null,
        decimal? minTotalSpent = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default);
    
    Task<AdminCustomerDetailDto?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    
    Task<AdminCustomerDto> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    
    Task DeleteCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    
    Task<AdminCustomerDto> AddCustomerNoteAsync(Guid customerId, AddCustomerNoteRequest request, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<CustomerOrderSummaryDto>> GetCustomerOrdersAsync(Guid customerId, int count = 10, CancellationToken cancellationToken = default);
    
    Task<CustomerStatisticsDto> GetCustomerStatisticsAsync(CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkDeleteCustomersAsync(IReadOnlyList<Guid> customerIds, CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkUpdateTierAsync(IReadOnlyList<Guid> customerIds, string tier, CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportCustomersAsync(string format, CancellationToken cancellationToken = default);
}
