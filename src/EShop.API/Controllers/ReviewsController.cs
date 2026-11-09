using EShop.Application.Commands.Reviews;
using EShop.Application.Queries.Reviews;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get review by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReviewDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDetailsDto>> GetReview(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReviewQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Get product reviews
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(ProductReviewsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductReviewsResult>> GetProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetProductReviewsQuery(productId, page, pageSize, sortBy), 
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a product review
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateReviewResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateReviewResult>> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateReviewCommand(
            request.ProductId,
            request.CustomerId,
            request.CustomerName,
            request.Rating,
            request.Title,
            request.Content,
            request.OrderId,
            request.Images);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetReview), new { id = result.ReviewId }, result);
    }

    /// <summary>
    /// Update review
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReviewOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewOperationResult>> UpdateReview(
        Guid id,
        [FromBody] UpdateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateReviewCommand(
            id,
            request.Rating,
            request.Title,
            request.Content);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Add image to review
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(ReviewOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewOperationResult>> AddImage(
        Guid id,
        [FromBody] AddReviewImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddReviewImageCommand(id, request.ImageUrl, request.Caption), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Vote on review (helpful/not helpful)
    /// </summary>
    [HttpPost("{id:guid}/vote")]
    [ProducesResponseType(typeof(ReviewOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewOperationResult>> VoteOnReview(
        Guid id,
        [FromBody] VoteOnReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VoteOnReviewCommand(id, request.CustomerId, request.IsHelpful), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Add seller response to review
    /// </summary>
    [HttpPost("{id:guid}/response")]
    [ProducesResponseType(typeof(ReviewOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewOperationResult>> AddResponse(
        Guid id,
        [FromBody] AddResponseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddSellerResponseCommand(id, request.Response, request.ResponderName), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Approve review (Admin)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ReviewOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewOperationResult>> ApproveReview(
        Guid id,
        [FromBody] ApproveReviewRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ApproveReviewCommand(id, request?.Notes), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Reject review (Admin)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ReviewOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewOperationResult>> RejectReview(
        Guid id,
        [FromBody] RejectReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RejectReviewCommand(id, request.Reason), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Flag review for moderation
    /// </summary>
    [HttpPost("{id:guid}/flag")]
    [ProducesResponseType(typeof(ReviewOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewOperationResult>> FlagReview(
        Guid id,
        [FromBody] FlagReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new FlagReviewCommand(id, request.Reason), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Get customer reviews
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(List<ReviewSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReviewSummaryDto>>> GetCustomerReviews(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerReviewsQuery(customerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get pending moderation (Admin)
    /// </summary>
    [HttpGet("pending-moderation")]
    [ProducesResponseType(typeof(List<ReviewSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReviewSummaryDto>>> GetPendingModeration(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingModerationQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get product rating summary
    /// </summary>
    [HttpGet("product/{productId:guid}/summary")]
    [ProducesResponseType(typeof(RatingSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RatingSummaryDto>> GetRatingSummary(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRatingSummaryQuery(productId), cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record CreateReviewRequest(
    Guid ProductId,
    Guid CustomerId,
    string CustomerName,
    int Rating,
    string Title,
    string Content,
    Guid? OrderId = null,
    List<string>? Images = null);

public record UpdateReviewRequest(
    int Rating,
    string Title,
    string Content);

public record AddReviewImageRequest(string ImageUrl, string? Caption = null);
public record VoteOnReviewRequest(Guid CustomerId, bool IsHelpful);
public record AddResponseRequest(string Response, string ResponderName);
public record ApproveReviewRequest(string? Notes = null);
public record RejectReviewRequest(string Reason);
public record FlagReviewRequest(string Reason);
