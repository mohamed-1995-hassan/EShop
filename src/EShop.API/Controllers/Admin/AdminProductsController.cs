using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CreateProductRequest = EShop.Application.DTOs.CreateProductRequest;
using UpdateProductRequest = EShop.Application.DTOs.UpdateProductRequest;

namespace EShop.API.Controllers.Admin;

/// <summary>
/// Admin Product Management
/// </summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin,Manager")]
[Produces("application/json")]
[Tags("Admin Products")]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _productService;
    private readonly ILogger<AdminProductsController> _logger;

    public AdminProductsController(
        IAdminProductService productService,
        ILogger<AdminProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductsAsync(
            page, pageSize, search, categoryId, isActive, isFeatured,
            stockStatus, minPrice, maxPrice, sortBy, sortDescending, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDetailDto>> GetProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminProductDto>> CreateProduct(
        [FromBody] EShop.Application.DTOs.CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _productService.CreateProductAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Update product
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDto>> UpdateProduct(
        Guid id,
        [FromBody] EShop.Application.DTOs.UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _productService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Delete product (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _productService.DeleteProductAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Update product inventory
    /// </summary>
    [HttpPut("{id:guid}/inventory")]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDto>> UpdateInventory(
        Guid id,
        [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _productService.UpdateInventoryAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Duplicate product
    /// </summary>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDto>> DuplicateProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _productService.DuplicateProductAsync(id, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Get product statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ProductStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductStatisticsDto>> GetStatistics(
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductStatisticsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk delete products
    /// </summary>
    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkDelete(
        [FromBody] BulkProductIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.BulkDeleteProductsAsync(request.ProductIds, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk activate/deactivate products
    /// </summary>
    [HttpPost("bulk-activate")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkActivate(
        [FromBody] BulkActivateProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.BulkActivateProductsAsync(request.ProductIds, request.Activate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk update prices
    /// </summary>
    [HttpPost("bulk-update-prices")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkUpdatePrices(
        [FromBody] BulkPriceUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.BulkUpdatePricesAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Export products
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var data = await _productService.ExportProductsAsync(format, cancellationToken);
        var contentType = format.ToLowerInvariant() == "csv" ? "text/csv" : "application/json";
        var extension = format.ToLowerInvariant() == "csv" ? "csv" : "json";
        return File(data, contentType, $"products_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}");
    }

    /// <summary>
    /// Import products
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> ImportProducts(
        IFormFile file,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream();
        var result = await _productService.ImportProductsAsync(stream, format, cancellationToken);
        return Ok(result);
    }

    #region Categories

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryDto>>> GetCategories(
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create category
    /// </summary>
    [HttpPost("categories")]
    [ProducesResponseType(typeof(AdminCategoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminCategoryDto>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.CreateCategoryAsync(request.Name, request.Description, request.ParentId, cancellationToken);
        return Created($"/api/admin/products/categories/{result.Id}", result);
    }

    /// <summary>
    /// Update category
    /// </summary>
    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(typeof(AdminCategoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminCategoryDto>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.UpdateCategoryAsync(id, request.Name, request.Description, request.IsActive, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete category
    /// </summary>
    [HttpDelete("categories/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _productService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    #endregion
}

#region Request DTOs

public class BulkProductIdsRequest
{
    public IReadOnlyList<Guid> ProductIds { get; init; } = Array.Empty<Guid>();
}

public class BulkActivateProductsRequest
{
    public IReadOnlyList<Guid> ProductIds { get; init; } = Array.Empty<Guid>();
    public bool Activate { get; init; }
}

public class CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}

#endregion
