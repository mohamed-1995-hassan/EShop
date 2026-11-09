using EShop.Application.Commands.Vendors;
using EShop.Application.Queries.Vendors;
using EShop.Domain.Aggregates.VendorAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendorsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VendorDto>> Get(Guid id)
    {
        var vendor = await _mediator.Send(new GetVendorQuery(id));
        return vendor != null ? Ok(vendor) : NotFound();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> GetActive([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var vendors = await _mediator.Send(new GetActiveVendorsQuery(skip, take));
        return Ok(vendors);
    }

    [HttpGet("pending-approval")]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> GetPendingApproval()
    {
        var vendors = await _mediator.Send(new GetPendingApprovalVendorsQuery());
        return Ok(vendors);
    }

    [HttpGet("tier/{tier}")]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> GetByTier(VendorTier tier)
    {
        var vendors = await _mediator.Send(new GetVendorsByTierQuery(tier));
        return Ok(vendors);
    }

    [HttpGet("category/{categoryId:guid}")]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> GetByCategory(Guid categoryId)
    {
        var vendors = await _mediator.Send(new GetVendorsByCategoryQuery(categoryId));
        return Ok(vendors);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Register([FromBody] RegisterVendorRequest request)
    {
        var id = await _mediator.Send(new RegisterVendorCommand(
            request.BusinessName,
            request.LegalName,
            request.ContactEmail,
            request.ContactPhone,
            request.Street,
            request.City,
            request.Country,
            request.PostalCode,
            request.TaxId));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<IActionResult> SubmitForApproval(Guid id)
    {
        await _mediator.Send(new SubmitVendorForApprovalCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveVendorRequest request)
    {
        await _mediator.Send(new ApproveVendorCommand(id, request.ApprovedBy, request.Notes));
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectVendorRequest request)
    {
        await _mediator.Send(new RejectVendorCommand(id, request.RejectedBy, request.Reason));
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendVendorRequest request)
    {
        await _mediator.Send(new SuspendVendorCommand(id, request.SuspendedBy, request.Reason));
        return NoContent();
    }

    [HttpPost("{vendorId:guid}/categories")]
    public async Task<ActionResult<Guid>> AddCategory(Guid vendorId, [FromBody] AddVendorCategoryRequest request)
    {
        var id = await _mediator.Send(new AddVendorCategoryCommand(
            vendorId,
            request.CategoryId,
            request.CategoryName,
            request.CustomCommissionRate));
        return Ok(id);
    }

    [HttpPost("{vendorId:guid}/documents")]
    public async Task<ActionResult<Guid>> AddDocument(Guid vendorId, [FromBody] AddVendorDocumentRequest request)
    {
        var id = await _mediator.Send(new AddVendorDocumentCommand(
            vendorId,
            request.Type,
            request.DocumentUrl,
            request.Description));
        return Ok(id);
    }

    [HttpPost("{vendorId:guid}/sales")]
    public async Task<IActionResult> RecordSale(Guid vendorId, [FromBody] RecordSaleRequest request)
    {
        await _mediator.Send(new RecordVendorSaleCommand(
            vendorId,
            request.ProductsSold,
            request.Revenue,
            request.Currency));
        return NoContent();
    }
}

// Request DTOs
public record RegisterVendorRequest(
    string BusinessName,
    string LegalName,
    string ContactEmail,
    string ContactPhone,
    string Street,
    string City,
    string Country,
    string PostalCode,
    string? TaxId = null);

public record ApproveVendorRequest(Guid ApprovedBy, string? Notes = null);
public record RejectVendorRequest(Guid RejectedBy, string Reason);
public record SuspendVendorRequest(Guid SuspendedBy, string Reason);

public record AddVendorCategoryRequest(
    Guid CategoryId,
    string CategoryName,
    decimal? CustomCommissionRate = null);

public record AddVendorDocumentRequest(
    DocumentType Type,
    string DocumentUrl,
    string? Description = null);

public record RecordSaleRequest(int ProductsSold, decimal Revenue, string Currency);
