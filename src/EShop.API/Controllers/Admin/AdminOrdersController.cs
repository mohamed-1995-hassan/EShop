using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ProcessRefundRequest = EShop.Application.DTOs.ProcessRefundRequest;

namespace EShop.API.Controllers.Admin;

/// <summary>
/// Admin Order Management
/// </summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin,Manager,Support")]
[Produces("application/json")]
[Tags("Admin Orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _orderService;
    private readonly ILogger<AdminOrdersController> _logger;

    public AdminOrdersController(
        IAdminOrderService orderService,
        ILogger<AdminOrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminOrderDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersAsync(
            page, pageSize, search, status, paymentStatus,
            startDate, endDate, customerId, sortBy, sortDescending, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDetailDto>> GetOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Get order by order number
    /// </summary>
    [HttpGet("by-number/{orderNumber}")]
    [ProducesResponseType(typeof(AdminOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDetailDto>> GetOrderByNumber(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Update order status
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Update shipping information
    /// </summary>
    [HttpPut("{id:guid}/shipping")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> UpdateShipping(
        Guid id,
        [FromBody] UpdateShippingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderService.UpdateShippingAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Add note to order
    /// </summary>
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> AddNote(
        Guid id,
        [FromBody] AddOrderNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderService.AddOrderNoteAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Process refund for order
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> ProcessRefund(
        Guid id,
        [FromBody] EShop.Application.DTOs.ProcessRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderService.ProcessRefundAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Cancel order
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _orderService.CancelOrderAsync(id, request?.Reason, cancellationToken);
            return Ok(new { message = "Order cancelled" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Get order statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(OrderStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderStatisticsDto>> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrderStatisticsAsync(startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk update order status
    /// </summary>
    [HttpPost("bulk-update-status")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkUpdateStatus(
        [FromBody] BulkUpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.BulkUpdateStatusAsync(request.OrderIds, request.Status, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Export orders
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] string format = "csv",
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _orderService.ExportOrdersAsync(format, status, startDate, endDate, cancellationToken);
        var contentType = format.ToLowerInvariant() == "csv" ? "text/csv" : "application/json";
        var extension = format.ToLowerInvariant() == "csv" ? "csv" : "json";
        return File(data, contentType, $"orders_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}");
    }
}

#region Request DTOs

public class CancelOrderRequest
{
    public string? Reason { get; init; }
}

public class BulkUpdateOrderStatusRequest
{
    public IReadOnlyList<Guid> OrderIds { get; init; } = Array.Empty<Guid>();
    public string Status { get; init; } = string.Empty;
}

#endregion
