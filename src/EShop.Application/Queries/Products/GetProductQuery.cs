using EShop.Domain.Aggregates.ProductAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Products;

// ============================================
// GET PRODUCT BY ID
// ============================================
public record GetProductQuery(Guid ProductId) : IRequest<ProductDetailsDto?>;

public record ProductDetailsDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    int LowStockThreshold,
    bool IsDiscontinued,
    bool InStock,
    List<string> Categories,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDetailsDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDetailsDto?> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null) return null;

        return MapToDto(product);
    }

    private static ProductDetailsDto MapToDto(Product product)
    {
        return new ProductDetailsDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity,
            product.LowStockThreshold,
            product.IsDiscontinued,
            product.StockQuantity > 0 && !product.IsDiscontinued,
            product.Categories.Select(c => c.Name).ToList(),
            product.CreatedAt,
            product.UpdatedAt);
    }
}

// ============================================
// GET PRODUCTS (PAGINATED WITH FILTERS)
// ============================================
public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SearchTerm = null,
    bool IncludeOutOfStock = false,
    string? SortBy = null,
    bool Descending = false) : IRequest<PagedProductsResult>;

public record PagedProductsResult(
    List<ProductListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record ProductListItemDto(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool InStock,
    string? ThumbnailUrl);

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedProductsResult>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedProductsResult> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Use search if term provided, otherwise get by category or low stock as fallback
        IReadOnlyList<Product> allProducts;
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            allProducts = await _productRepository.SearchAsync(request.SearchTerm, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Category))
        {
            allProducts = await _productRepository.GetByCategoryAsync(request.Category, cancellationToken);
        }
        else
        {
            // Get low stock as a workaround since GetAllAsync doesn't exist
            allProducts = await _productRepository.GetLowStockAsync(cancellationToken);
        }

        // Apply filters
        var filtered = allProducts.AsEnumerable();

        if (!request.IncludeOutOfStock)
            filtered = filtered.Where(p => p.StockQuantity > 0 && !p.IsDiscontinued);

        if (!string.IsNullOrWhiteSpace(request.Category))
            filtered = filtered.Where(p => p.Categories.Any(c => 
                c.Name.Equals(request.Category, StringComparison.OrdinalIgnoreCase)));

        if (request.MinPrice.HasValue)
            filtered = filtered.Where(p => p.Price.Amount >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            filtered = filtered.Where(p => p.Price.Amount <= request.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            filtered = filtered.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term));
        }

        // Apply sorting
        filtered = request.SortBy?.ToLower() switch
        {
            "price" => request.Descending 
                ? filtered.OrderByDescending(p => p.Price.Amount) 
                : filtered.OrderBy(p => p.Price.Amount),
            "name" => request.Descending 
                ? filtered.OrderByDescending(p => p.Name) 
                : filtered.OrderBy(p => p.Name),
            "created" => request.Descending 
                ? filtered.OrderByDescending(p => p.CreatedAt) 
                : filtered.OrderBy(p => p.CreatedAt),
            _ => filtered.OrderBy(p => p.Name)
        };

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var pagedItems = filteredList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Price.Amount,
                p.Price.Currency,
                p.StockQuantity,
                p.StockQuantity > 0 && !p.IsDiscontinued,
                null))
            .ToList();

        return new PagedProductsResult(
            pagedItems,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages);
    }
}

// ============================================
// GET LOW STOCK PRODUCTS
// ============================================
public record GetLowStockProductsQuery() : IRequest<List<LowStockProductDto>>;

public record LowStockProductDto(
    Guid Id,
    string Sku,
    string Name,
    int StockQuantity,
    int LowStockThreshold,
    bool IsOutOfStock);

public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, List<LowStockProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetLowStockProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<LowStockProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetLowStockAsync(cancellationToken);

        return products
            .Where(p => !p.IsDiscontinued)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.StockQuantity,
                p.LowStockThreshold,
                p.StockQuantity == 0))
            .ToList();
    }
}
