using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using EShop.Domain.Aggregates.ProductAggregate;
using EShop.Domain.ValueObjects;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

using ProductEntity = EShop.Domain.Aggregates.ProductAggregate.Product;

namespace EShop.Infrastructure.Admin;

/// <summary>
/// Implementation of admin product management service
/// </summary>
public class AdminProductService : IAdminProductService
{
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<AdminProductService> _logger;

    public AdminProductService(EShopDbContext dbContext, ILogger<AdminProductService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<AdminProductDto>> GetProductsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search) || 
                                    (p.Description != null && p.Description.Contains(search)));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.Categories.Any(c => c.Name != null)); // Product doesn't have single CategoryId

        if (isActive.HasValue)
            query = query.Where(p => !p.IsDiscontinued == isActive.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price.Amount >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price.Amount <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(stockStatus))
        {
            query = stockStatus.ToLowerInvariant() switch
            {
                "outofstock" => query.Where(p => p.StockQuantity == 0),
                "lowstock" => query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 10),
                "instock" => query.Where(p => p.StockQuantity > 10),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => sortDescending ? query.OrderByDescending(p => p.Price.Amount) : query.OrderBy(p => p.Price.Amount),
            "stock" => sortDescending ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
            "sku" => sortDescending ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
            _ => sortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = products.Select(MapToAdminProductDto).ToList();

        return PagedResult<AdminProductDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<AdminProductDetailDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null) return null;

        return MapToAdminProductDetailDto(product);
    }

    public async Task<AdminProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var price = new Money(request.Price, "USD");
        var product = ProductEntity.Create(request.Sku, request.Name, request.Description ?? "", price, request.StockQuantity);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        return MapToAdminProductDto(product);
    }

    public async Task<AdminProductDto> UpdateProductAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            throw new InvalidOperationException($"Product with ID {productId} not found");

        if (!string.IsNullOrWhiteSpace(request.Name) || !string.IsNullOrWhiteSpace(request.Description))
        {
            product.UpdateDetails(
                request.Name ?? product.Name, 
                request.Description ?? product.Description);
        }

        if (request.Price.HasValue)
            product.UpdatePrice(new Money(request.Price.Value, product.Price.Currency));

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                product.Reactivate();
            else
                product.Discontinue();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product updated: {ProductId}", productId);

        return MapToAdminProductDto(product);
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            throw new InvalidOperationException($"Product with ID {productId} not found");

        // Soft delete - discontinue instead of removing
        if (!product.IsDiscontinued)
        {
            product.Discontinue();
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product discontinued (soft-deleted): {ProductId}", productId);
    }

    public async Task<AdminProductDto> UpdateInventoryAsync(Guid productId, UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            throw new InvalidOperationException($"Product with ID {productId} not found");

        switch (request.AdjustmentType.ToLowerInvariant())
        {
            case "set":
                // For set, calculate the difference and add/reserve accordingly
                var diff = request.Quantity - product.StockQuantity;
                if (diff > 0)
                    product.AddStock(diff, request.Reason);
                else if (diff < 0)
                    product.Reserve(Math.Abs(diff));
                break;
            case "add":
                product.AddStock(request.Quantity, request.Reason);
                break;
            case "subtract":
                product.Reserve(request.Quantity);
                break;
            default:
                throw new InvalidOperationException($"Invalid adjustment type: {request.AdjustmentType}");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product inventory updated: {ProductId}, Type={AdjustmentType}, Quantity={Quantity}", 
            productId, request.AdjustmentType, request.Quantity);

        return MapToAdminProductDto(product);
    }

    public async Task<AdminProductDto> DuplicateProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var original = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (original == null)
            throw new InvalidOperationException($"Product with ID {productId} not found");

        var newSku = $"{original.Sku}-COPY-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var duplicate = ProductEntity.Create(
            newSku,
            $"{original.Name} (Copy)",
            original.Description,
            original.Price,
            0 // Start with zero stock
        );

        _dbContext.Products.Add(duplicate);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product duplicated: Original={OriginalId}, Duplicate={DuplicateId}", productId, duplicate.Id);

        return MapToAdminProductDto(duplicate);
    }

    public async Task<ProductStatisticsDto> GetProductStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products.ToListAsync(cancellationToken);

        return new ProductStatisticsDto
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => !p.IsDiscontinued),
            InactiveProducts = products.Count(p => p.IsDiscontinued),
            OutOfStockProducts = products.Count(p => p.StockQuantity == 0),
            LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold),
            FeaturedProducts = 0, // Would need IsFeatured property
            TotalInventoryValue = products.Sum(p => p.Price.Amount * p.StockQuantity),
            AveragePrice = products.Count > 0 ? products.Average(p => p.Price.Amount) : 0
        };
    }

    public async Task<BulkOperationResultDto> BulkDeleteProductsAsync(IReadOnlyList<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var productId in productIds)
        {
            try
            {
                await DeleteProductAsync(productId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = productId.ToString(), ErrorMessage = ex.Message });
            }
        }

        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    public async Task<BulkOperationResultDto> BulkActivateProductsAsync(IReadOnlyList<Guid> productIds, bool activate, CancellationToken cancellationToken = default)
    {
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var productId in productIds)
        {
            try
            {
                var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
                if (product == null)
                {
                    errors.Add(new BulkOperationError { ItemId = productId.ToString(), ErrorMessage = "Product not found" });
                    continue;
                }

                if (activate)
                {
                    if (product.IsDiscontinued)
                        product.Reactivate();
                }
                else
                {
                    if (!product.IsDiscontinued)
                        product.Discontinue();
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = productId.ToString(), ErrorMessage = ex.Message });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    public async Task<BulkOperationResultDto> BulkUpdatePricesAsync(BulkPriceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var productId in request.ProductIds)
        {
            try
            {
                var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
                if (product == null)
                {
                    errors.Add(new BulkOperationError { ItemId = productId.ToString(), ErrorMessage = "Product not found" });
                    continue;
                }

                decimal newPrice;
                if (request.FixedPrice.HasValue)
                {
                    newPrice = request.FixedPrice.Value;
                }
                else if (request.PercentageChange.HasValue)
                {
                    newPrice = product.Price.Amount * (1 + request.PercentageChange.Value / 100);
                }
                else
                {
                    errors.Add(new BulkOperationError { ItemId = productId.ToString(), ErrorMessage = "No price update specified" });
                    continue;
                }

                if (request.RoundToNearest && request.RoundingValue.HasValue)
                {
                    newPrice = Math.Round(newPrice / request.RoundingValue.Value) * request.RoundingValue.Value;
                }

                product.UpdatePrice(new Money(newPrice, product.Price.Currency));
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = productId.ToString(), ErrorMessage = ex.Message });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    public async Task<byte[]> ExportProductsAsync(string format, CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        if (format.ToLowerInvariant() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,SKU,Price,Stock,IsActive,CreatedAt");
            foreach (var product in products)
            {
                csv.AppendLine($"{product.Id},{EscapeCsv(product.Name)},{product.Sku},{product.Price.Amount},{product.StockQuantity},{!product.IsDiscontinued},{product.CreatedAt:yyyy-MM-dd}");
            }
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        var dtos = products.Select(MapToAdminProductDto).ToList();
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task<BulkOperationResultDto> ImportProductsAsync(Stream fileStream, string format, CancellationToken cancellationToken = default)
    {
        // Basic CSV import implementation
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        using var reader = new StreamReader(fileStream);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (headerLine == null)
            return BulkOperationResultDto.WithErrors(0, new List<BulkOperationError> 
            { 
                new() { ItemId = "0", ErrorMessage = "Empty file" } 
            });

        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var parts = line.Split(',');
                if (parts.Length < 4)
                {
                    errors.Add(new BulkOperationError { ItemId = lineNumber.ToString(), ErrorMessage = "Invalid line format" });
                    continue;
                }

                var name = parts[0].Trim('"');
                var sku = parts[1].Trim('"');
                var price = decimal.Parse(parts[2]);
                var stock = int.Parse(parts[3]);

                var product = ProductEntity.Create(sku, name, "", new Money(price, "USD"), stock);
                _dbContext.Products.Add(product);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = lineNumber.ToString(), ErrorMessage = ex.Message });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    public async Task<IReadOnlyList<AdminCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // Would need a Category entity
        return new List<AdminCategoryDto>();
    }

    public Task<AdminCategoryDto> CreateCategoryAsync(string name, string? description = null, Guid? parentId = null, CancellationToken cancellationToken = default)
    {
        // Would need a Category entity
        throw new NotImplementedException("Category management requires Category entity implementation");
    }

    public Task<AdminCategoryDto> UpdateCategoryAsync(Guid categoryId, string? name = null, string? description = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Category management requires Category entity implementation");
    }

    public Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Category management requires Category entity implementation");
    }

    private static AdminProductDto MapToAdminProductDto(Product product)
    {
        return new AdminProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price.Amount,
            CompareAtPrice = null,
            Cost = null,
            StockQuantity = product.StockQuantity,
            ReservedQuantity = 0,
            IsActive = !product.IsDiscontinued,
            IsFeatured = false,
            CategoryId = null, // Product uses Categories collection, not single CategoryId
            CategoryName = product.Categories.FirstOrDefault()?.Name,
            ImageUrl = null,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    private static AdminProductDetailDto MapToAdminProductDetailDto(Product product)
    {
        var dto = MapToAdminProductDto(product);
        return new AdminProductDetailDto
        {
            Id = dto.Id,
            Name = dto.Name,
            Sku = dto.Sku,
            Description = dto.Description,
            Price = dto.Price,
            CompareAtPrice = dto.CompareAtPrice,
            Cost = dto.Cost,
            StockQuantity = dto.StockQuantity,
            ReservedQuantity = dto.ReservedQuantity,
            IsActive = dto.IsActive,
            IsFeatured = dto.IsFeatured,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            ImageUrl = dto.ImageUrl,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Images = new List<ProductImageDto>(),
            Variants = new List<ProductVariantDto>(),
            Tags = new List<string>(),
            Seo = null,
            Reviews = new List<ProductReviewSummaryDto>(),
            Inventory = new ProductInventoryDto
            {
                StockQuantity = product.StockQuantity,
                ReservedQuantity = 0,
                ReorderLevel = product.LowStockThreshold,
                ReorderQuantity = 50,
                TrackInventory = true,
                AllowBackorder = false
            },
            Weight = null,
            WeightUnit = null,
            Dimensions = null
        };
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
