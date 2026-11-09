using EShop.Application.Commands.Returns;
using EShop.Application.Queries.Returns;
using EShop.Domain.Aggregates.ReturnAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReturnsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReturnsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get return request by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReturnDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnDetailsDto>> GetReturn(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReturnQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Get return by return number
    /// </summary>
    [HttpGet("number/{returnNumber}")]
    [ProducesResponseType(typeof(ReturnDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnDetailsDto>> GetReturnByNumber(
        string returnNumber,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReturnByNumberQuery(returnNumber), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create a return request
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateReturnResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateReturnResult>> CreateReturn(
        [FromBody] CreateReturnRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateReturnCommand(
            request.OrderId,
            request.CustomerId,
            request.Type,
            request.Reason,
            request.PreferredRefundMethod,
            request.CustomerNotes,
            request.Items?.Select(i => new CreateReturnItemDto(
                i.OrderLineId,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.Reason,
                i.ReasonDetails)).ToList());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetReturn), new { id = result.ReturnId }, result);
    }

    /// <summary>
    /// Add item to return request
    /// </summary>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> AddItem(
        Guid id,
        [FromBody] AddReturnItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddReturnItemCommand(
            id,
            request.OrderLineId,
            request.ProductId,
            request.ProductName,
            request.Quantity,
            request.UnitPrice,
            request.Reason,
            request.ReasonDetails);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Upload images for return
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> AddImage(
        Guid id,
        [FromBody] AddReturnImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddReturnImageCommand(id, request.ImageUrl, request.Description), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Submit return request for approval
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> SubmitReturn(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubmitReturnCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Approve return request (Admin)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> ApproveReturn(
        Guid id,
        [FromBody] ApproveReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ApproveReturnCommand(id, request.ApprovedBy, request.Notes), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Reject return request (Admin)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> RejectReturn(
        Guid id,
        [FromBody] RejectReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RejectReturnCommand(id, request.RejectedBy, request.Reason), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Generate return shipping label
    /// </summary>
    [HttpPost("{id:guid}/label")]
    [ProducesResponseType(typeof(ReturnLabelResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnLabelResult>> GenerateLabel(
        Guid id,
        [FromBody] GenerateLabelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateReturnLabelCommand(id, request.TrackingNumber, request.LabelUrl), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Mark return as shipped by customer
    /// </summary>
    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> MarkShipped(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkReturnShippedCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Mark return as received at warehouse
    /// </summary>
    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> MarkReceived(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkReturnReceivedCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Start inspection
    /// </summary>
    [HttpPost("{id:guid}/inspect/start")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> StartInspection(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartInspectionCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Record inspection result for an item
    /// </summary>
    [HttpPost("{id:guid}/inspect/{itemId:guid}")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> RecordInspectionResult(
        Guid id,
        Guid itemId,
        [FromBody] InspectionResultRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RecordInspectionResultCommand(id, itemId, request.Result, request.Notes), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Complete inspection
    /// </summary>
    [HttpPost("{id:guid}/inspect/complete")]
    [ProducesResponseType(typeof(ReturnOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnOperationResult>> CompleteInspection(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteInspectionCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Process refund
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(ReturnRefundResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnRefundResult>> ProcessRefund(
        Guid id,
        [FromBody] ProcessReturnRefundRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ProcessReturnRefundCommand(id, request.ProcessedBy), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Get customer returns
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(List<ReturnSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReturnSummaryDto>>> GetCustomerReturns(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerReturnsQuery(customerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get pending approvals (Admin)
    /// </summary>
    [HttpGet("pending-approval")]
    [ProducesResponseType(typeof(List<ReturnSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReturnSummaryDto>>> GetPendingApprovals(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingApprovalsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get returns awaiting inspection (Admin)
    /// </summary>
    [HttpGet("awaiting-inspection")]
    [ProducesResponseType(typeof(List<ReturnSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReturnSummaryDto>>> GetAwaitingInspection(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAwaitingInspectionQuery(), cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record CreateReturnRequest(
    Guid OrderId,
    Guid CustomerId,
    ReturnType Type,
    string Reason,
    RefundMethod PreferredRefundMethod,
    string? CustomerNotes = null,
    List<ReturnItemDto>? Items = null);

public record ReturnItemDto(
    Guid OrderLineId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    ReturnReason Reason,
    string? ReasonDetails = null);

public record AddReturnItemRequest(
    Guid OrderLineId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    ReturnReason Reason,
    string? ReasonDetails = null);

public record AddReturnImageRequest(string ImageUrl, string? Description = null);
public record ApproveReturnRequest(Guid ApprovedBy, string? Notes = null);
public record RejectReturnRequest(Guid RejectedBy, string Reason);
public record GenerateLabelRequest(string TrackingNumber, string LabelUrl);
public record InspectionResultRequest(InspectionResult Result, string? Notes = null);
public record ProcessReturnRefundRequest(Guid ProcessedBy);
