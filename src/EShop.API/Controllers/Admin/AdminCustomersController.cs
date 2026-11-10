using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers.Admin;

/// <summary>
/// Admin Customer Management
/// </summary>
[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = "Admin,Manager,Support")]
[Produces("application/json")]
[Tags("Admin Customers")]
public class AdminCustomersController : ControllerBase
{
    private readonly IAdminCustomerService _customerService;
    private readonly ILogger<AdminCustomersController> _logger;

    public AdminCustomersController(
        IAdminCustomerService customerService,
        ILogger<AdminCustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Get all customers with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminCustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminCustomerDto>>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? tier = null,
        [FromQuery] DateTime? registeredAfter = null,
        [FromQuery] DateTime? registeredBefore = null,
        [FromQuery] decimal? minTotalSpent = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetCustomersAsync(
            page, pageSize, search, isActive, tier,
            registeredAfter, registeredBefore, minTotalSpent,
            sortBy, sortDescending, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminCustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCustomerDetailDto>> GetCustomer(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Update customer profile
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminCustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCustomerDto>> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _customerService.UpdateCustomerAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Delete customer
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _customerService.DeleteCustomerAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Add note to customer profile
    /// </summary>
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType(typeof(AdminCustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCustomerDto>> AddNote(
        Guid id,
        [FromBody] AddCustomerNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _customerService.AddCustomerNoteAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Get customer order history
    /// </summary>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerOrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerOrderSummaryDto>>> GetCustomerOrders(
        Guid id,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetCustomerOrdersAsync(id, count, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get customer statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(CustomerStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerStatisticsDto>> GetStatistics(
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetCustomerStatisticsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk delete customers
    /// </summary>
    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkDelete(
        [FromBody] BulkCustomerIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.BulkDeleteCustomersAsync(request.CustomerIds, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk update customer tier
    /// </summary>
    [HttpPost("bulk-update-tier")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkUpdateTier(
        [FromBody] BulkUpdateTierRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.BulkUpdateTierAsync(request.CustomerIds, request.Tier, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Export customers
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCustomers(
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var data = await _customerService.ExportCustomersAsync(format, cancellationToken);
        var contentType = format.ToLowerInvariant() == "csv" ? "text/csv" : "application/json";
        var extension = format.ToLowerInvariant() == "csv" ? "csv" : "json";
        return File(data, contentType, $"customers_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}");
    }
}

#region Request DTOs

public class BulkCustomerIdsRequest
{
    public IReadOnlyList<Guid> CustomerIds { get; init; } = Array.Empty<Guid>();
}

public class BulkUpdateTierRequest
{
    public IReadOnlyList<Guid> CustomerIds { get; init; } = Array.Empty<Guid>();
    public string Tier { get; init; } = string.Empty;
}

#endregion
