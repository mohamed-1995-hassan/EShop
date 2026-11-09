using EShop.Application.Commands.Products;
using EShop.Application.Queries.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of products with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedProductsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedProductsResult>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeOutOfStock = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery(
            page, pageSize, category, minPrice, maxPrice, 
            search, includeOutOfStock, sortBy, descending);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> GetProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProductResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateProductResult>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Sku,
            request.Name,
            request.Description,
            request.Price,
            request.Currency ?? "KWD",
            request.InitialStock,
            request.LowStockThreshold,
            request.Categories);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetProduct), new { id = result.ProductId }, result);
    }

    /// <summary>
    /// Update product details
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Currency ?? "KWD");
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorMessage == "Product not found")
                return NotFound();
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });
        }

        return Ok();
    }

    /// <summary>
    /// Add stock to product
    /// </summary>
    [HttpPost("{id:guid}/stock")]
    [ProducesResponseType(typeof(AddStockResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddStockResult>> AddStock(
        Guid id,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddStockCommand(id, request.Quantity, request.Reference);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Discontinue a product
    /// </summary>
    [HttpPost("{id:guid}/discontinue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DiscontinueProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DiscontinueProductCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok();
    }

    /// <summary>
    /// Get products with low stock (Admin)
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(List<LowStockProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LowStockProductDto>>> GetLowStockProducts(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLowStockProductsQuery(), cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record CreateProductRequest(
    string Sku,
    string Name,
    string Description,
    decimal Price,
    string? Currency,
    int InitialStock,
    int LowStockThreshold,
    List<string>? Categories = null);

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal? Price = null,
    string? Currency = null);

public record AddStockRequest(int Quantity, string? Reference = null);
