using EShop.Application.DTOs;

namespace EShop.Application.Interfaces.Admin;

/// <summary>
/// Service interface for admin product management operations
/// </summary>
public interface IAdminProductService
{
    Task<PagedResult<AdminProductDto>> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        Guid? categoryId = null,
        bool? isActive = null,
        bool? isFeatured = null,
        string? stockStatus = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default);
    
    Task<AdminProductDetailDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    
    Task<AdminProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    
    Task<AdminProductDto> UpdateProductAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    
    Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);
    
    Task<AdminProductDto> UpdateInventoryAsync(Guid productId, UpdateInventoryRequest request, CancellationToken cancellationToken = default);
    
    Task<AdminProductDto> DuplicateProductAsync(Guid productId, CancellationToken cancellationToken = default);
    
    Task<ProductStatisticsDto> GetProductStatisticsAsync(CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkDeleteProductsAsync(IReadOnlyList<Guid> productIds, CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkActivateProductsAsync(IReadOnlyList<Guid> productIds, bool activate, CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkUpdatePricesAsync(BulkPriceUpdateRequest request, CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportProductsAsync(string format, CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> ImportProductsAsync(Stream fileStream, string format, CancellationToken cancellationToken = default);
    
    // Category operations
    Task<IReadOnlyList<AdminCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    
    Task<AdminCategoryDto> CreateCategoryAsync(string name, string? description = null, Guid? parentId = null, CancellationToken cancellationToken = default);
    
    Task<AdminCategoryDto> UpdateCategoryAsync(Guid categoryId, string? name = null, string? description = null, bool? isActive = null, CancellationToken cancellationToken = default);
    
    Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
