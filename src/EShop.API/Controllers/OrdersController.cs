using EShop.Application.DTOs;
using EShop.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

/// <summary>
/// Orders API Controller
/// 
/// DEMO FLOW:
/// 1. POST /api/orders - Create order
/// 2. POST /api/orders/{id}/items - Add items
/// 3. POST /api/orders/{id}/place - Place order (triggers events!)
/// 4. POST /api/orders/{id}/ship - Ship order
/// 5. POST /api/orders/{id}/cancel - Cancel order
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderAppService _orderService;
    private readonly ILogger<OrdersController> _logger;
    
    public OrdersController(
        IOrderAppService orderService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all orders
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllOrdersAsync(cancellationToken);
        return Ok(orders);
    }
    
    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderAsync(id, cancellationToken);
        return order == null ? NotFound() : Ok(order);
    }
    
    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var orderId = await _orderService.CreateOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = orderId }, orderId);
    }
    
    /// <summary>
    /// Add item to order
    /// </summary>
    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult> AddItem(Guid id, [FromBody] AddOrderItemDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _orderService.AddItemAsync(id, dto, cancellationToken);
            return Ok(new { message = "Item added successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("Cannot"))
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Place an order - THIS IS THE KEY DEMO ENDPOINT
    /// Watch the console for domain events being handled!
    /// </summary>
    [HttpPost("{id:guid}/place")]
    public async Task<ActionResult> Place(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _orderService.PlaceOrderAsync(id, cancellationToken);
            return Ok(new { 
                message = "Order placed successfully!",
                note = "Check the console - domain events have triggered handlers!"
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("Cannot") || ex.Message.Contains("not in"))
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Ship an order
    /// </summary>
    [HttpPost("{id:guid}/ship")]
    public async Task<ActionResult> Ship(Guid id, [FromBody] ShipOrderDto? dto, CancellationToken cancellationToken)
    {
        try
        {
            await _orderService.ShipOrderAsync(id, dto?.TrackingNumber, dto?.Carrier, cancellationToken);
            return Ok(new { message = "Order shipped successfully!" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("must be"))
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelOrderDto? dto, CancellationToken cancellationToken)
    {
        try
        {
            await _orderService.CancelOrderAsync(id, dto?.Reason ?? "Customer requested", cancellationToken);
            return Ok(new { message = "Order cancelled successfully!" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("Cannot"))
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Demo endpoint: Create a complete order in one call
    /// </summary>
    [HttpPost("demo")]
    public async Task<ActionResult> CreateDemoOrder(CancellationToken cancellationToken)
    {
        _logger.LogInformation("╔══════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║         DDD DEMO - COMPLETE ORDER FLOW                   ║");
        _logger.LogInformation("╚══════════════════════════════════════════════════════════╝");
        
        // 1. Create order
        var customerId = Guid.NewGuid();
        var createDto = new CreateOrderDto(
            customerId,
            "123 Main Street",
            "Kuwait City",
            "Kuwait",
            "12345"
        );
        var orderId = await _orderService.CreateOrderAsync(createDto, cancellationToken);
        _logger.LogInformation("📝 Step 1: Order created with ID {OrderId}", orderId);
        
        // 2. Add items
        await _orderService.AddItemAsync(orderId, new AddOrderItemDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "MacBook Pro 16\"",
            1299.99m,
            1,
            "KWD"
        ), cancellationToken);
        _logger.LogInformation("📦 Step 2: Added MacBook Pro");
        
        await _orderService.AddItemAsync(orderId, new AddOrderItemDto(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "iPhone 15 Pro",
            999.99m,
            2,
            "KWD"
        ), cancellationToken);
        _logger.LogInformation("📦 Step 3: Added 2x iPhone 15 Pro");
        
        // 3. Place order - THIS TRIGGERS ALL THE EVENTS
        _logger.LogInformation("🚀 Step 4: Placing order...");
        await _orderService.PlaceOrderAsync(orderId, cancellationToken);
        
        // Get final order
        var order = await _orderService.GetOrderAsync(orderId, cancellationToken);
        
        return Ok(new
        {
            message = "Demo order created and placed!",
            orderId,
            customerId,
            totalAmount = order?.TotalAmount,
            currency = order?.Currency,
            status = order?.Status,
            itemCount = order?.OrderLines.Count,
            note = "Check the console to see all domain events and handlers in action!"
        });
    }
}
