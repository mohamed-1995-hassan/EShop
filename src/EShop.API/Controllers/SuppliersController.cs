using EShop.Application.Commands.Suppliers;
using EShop.Application.Queries.Suppliers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuppliersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Get(Guid id)
    {
        var supplier = await _mediator.Send(new GetSupplierQuery(id));
        return supplier != null ? Ok(supplier) : NotFound();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<SupplierSummaryDto>>> GetActive()
    {
        var suppliers = await _mediator.Send(new GetActiveSupliersQuery());
        return Ok(suppliers);
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SupplierSummaryDto>>> GetForProduct(Guid productId)
    {
        var suppliers = await _mediator.Send(new GetSuppliersForProductQuery(productId));
        return Ok(suppliers);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSupplierRequest request)
    {
        var id = await _mediator.Send(new CreateSupplierCommand(request.Code, request.Name, request.ContactName, request.Email, request.Currency));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _mediator.Send(new ApproveSupplierCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendSupplierRequest request)
    {
        await _mediator.Send(new SuspendSupplierCommand(id, request.Reason));
        return NoContent();
    }

    [HttpPost("{id:guid}/products")]
    public async Task<ActionResult<Guid>> AddProduct(Guid id, [FromBody] AddSupplierProductRequest request)
    {
        var productId = await _mediator.Send(new AddSupplierProductCommand(id, request.ProductId, request.Sku, request.Name, request.UnitCost, request.Currency, request.MinOrderQty, request.LeadTimeDays));
        return Ok(productId);
    }

    [HttpPut("{id:guid}/products/{productId:guid}/cost")]
    public async Task<IActionResult> UpdateProductCost(Guid id, Guid productId, [FromBody] UpdateCostRequest request)
    {
        await _mediator.Send(new UpdateSupplierProductCostCommand(id, productId, request.NewCost, request.Currency));
        return NoContent();
    }

    [HttpPost("{id:guid}/delivery")]
    public async Task<IActionResult> RecordDelivery(Guid id, [FromBody] RecordDeliveryRequest request)
    {
        await _mediator.Send(new RecordSupplierDeliveryCommand(id, request.OnTime, request.HasQualityIssue));
        return NoContent();
    }

    // Purchase Order endpoints
    [HttpGet("purchase-orders/{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(Guid id)
    {
        var po = await _mediator.Send(new GetPurchaseOrderQuery(id));
        return po != null ? Ok(po) : NotFound();
    }

    [HttpGet("purchase-orders/pending-approval")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> GetPendingApprovalPOs()
    {
        var pos = await _mediator.Send(new GetPendingApprovalPOsQuery());
        return Ok(pos);
    }

    [HttpGet("purchase-orders/awaiting-delivery")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> GetAwaitingDeliveryPOs()
    {
        var pos = await _mediator.Send(new GetAwaitingDeliveryPOsQuery());
        return Ok(pos);
    }

    [HttpPost("purchase-orders")]
    public async Task<ActionResult<Guid>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        var id = await _mediator.Send(new CreatePurchaseOrderCommand(request.SupplierId, request.SupplierName, request.CreatedBy, request.Currency, request.WarehouseId));
        return CreatedAtAction(nameof(GetPurchaseOrder), new { id }, id);
    }

    [HttpPost("purchase-orders/{id:guid}/lines")]
    public async Task<ActionResult<Guid>> AddPurchaseOrderLine(Guid id, [FromBody] AddPurchaseOrderLineRequest request)
    {
        var lineId = await _mediator.Send(new AddPurchaseOrderLineCommand(id, request.ProductId, request.ProductName, request.Sku, request.Quantity, request.UnitCost, request.Currency));
        return Ok(lineId);
    }

    [HttpPost("purchase-orders/{id:guid}/submit")]
    public async Task<IActionResult> SubmitPurchaseOrder(Guid id)
    {
        await _mediator.Send(new SubmitPurchaseOrderCommand(id));
        return NoContent();
    }

    [HttpPost("purchase-orders/{id:guid}/approve")]
    public async Task<IActionResult> ApprovePurchaseOrder(Guid id, [FromBody] ApprovePurchaseOrderRequest request)
    {
        await _mediator.Send(new ApprovePurchaseOrderCommand(id, request.ApprovedBy));
        return NoContent();
    }

    [HttpPost("purchase-orders/{id:guid}/send")]
    public async Task<IActionResult> SendPurchaseOrder(Guid id)
    {
        await _mediator.Send(new SendPurchaseOrderCommand(id));
        return NoContent();
    }

    [HttpPost("purchase-orders/{id:guid}/receive")]
    public async Task<ActionResult<Guid>> ReceivePurchaseOrder(Guid id, [FromBody] ReceivePurchaseOrderRequest request)
    {
        var receiptId = await _mediator.Send(new ReceivePurchaseOrderCommand(id, request.ReceivedBy, request.QuantitiesReceived, request.Notes));
        return Ok(receiptId);
    }
}

public record CreateSupplierRequest(string Code, string Name, string ContactName, string Email, string Currency = "USD");
public record SuspendSupplierRequest(string Reason);
public record AddSupplierProductRequest(Guid ProductId, string Sku, string Name, decimal UnitCost, string Currency, int MinOrderQty = 1, int? LeadTimeDays = null);
public record UpdateCostRequest(decimal NewCost, string Currency);
public record RecordDeliveryRequest(bool OnTime, bool HasQualityIssue);

public record CreatePurchaseOrderRequest(Guid SupplierId, string SupplierName, Guid CreatedBy, string Currency, Guid? WarehouseId = null);
public record AddPurchaseOrderLineRequest(Guid ProductId, string ProductName, string Sku, int Quantity, decimal UnitCost, string Currency);
public record ApprovePurchaseOrderRequest(Guid ApprovedBy);
public record ReceivePurchaseOrderRequest(Guid ReceivedBy, Dictionary<Guid, int> QuantitiesReceived, string? Notes = null);
