using EShop.Domain.Aggregates.ReviewAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Reviews;

// ============================================
// CREATE REVIEW
// ============================================
public record CreateReviewCommand(
    Guid ProductId,
    Guid CustomerId,
    string CustomerName,
    int Rating,
    string Title,
    string Content,
    Guid? OrderId = null,
    List<string>? Images = null) : IRequest<CreateReviewResult>;

public record CreateReviewResult(
    bool Success,
    Guid? ReviewId,
    string? ErrorMessage);

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, CreateReviewResult>
{
    private readonly IReviewRepository _reviewRepository;

    public CreateReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<CreateReviewResult> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if customer already reviewed this product
            var existingReview = await _reviewRepository.GetByCustomerAndProductAsync(
                request.CustomerId, request.ProductId, cancellationToken);
            if (existingReview != null)
                return new CreateReviewResult(false, null, "You have already reviewed this product");

            var review = ProductReview.Create(
                request.ProductId,
                request.CustomerId,
                request.CustomerName,
                request.Rating,
                request.Title,
                request.Content,
                request.OrderId);

            if (request.Images != null)
            {
                foreach (var imageUrl in request.Images)
                    review.AddImage(imageUrl, null);
            }

            await _reviewRepository.AddAsync(review, cancellationToken);

            return new CreateReviewResult(true, review.Id, null);
        }
        catch (Exception ex)
        {
            return new CreateReviewResult(false, null, ex.Message);
        }
    }
}

// ============================================
// UPDATE REVIEW
// ============================================
public record UpdateReviewCommand(
    Guid ReviewId,
    int Rating,
    string Title,
    string Content) : IRequest<ReviewOperationResult>;

public record ReviewOperationResult(bool Success, string? Status, string? ErrorMessage);

public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewOperationResult>
{
    private readonly IReviewRepository _reviewRepository;

    public UpdateReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewOperationResult> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review == null)
                return new ReviewOperationResult(false, null, "Review not found");

            review.Update(request.Rating, request.Title, request.Content);
            await _reviewRepository.UpdateAsync(review, cancellationToken);

            return new ReviewOperationResult(true, review.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReviewOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// ADD REVIEW IMAGE
// ============================================
public record AddReviewImageCommand(Guid ReviewId, string ImageUrl, string? Caption = null) : IRequest<ReviewOperationResult>;

public class AddReviewImageCommandHandler : IRequestHandler<AddReviewImageCommand, ReviewOperationResult>
{
    private readonly IReviewRepository _reviewRepository;

    public AddReviewImageCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewOperationResult> Handle(AddReviewImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review == null)
                return new ReviewOperationResult(false, null, "Review not found");

            review.AddImage(request.ImageUrl, request.Caption);
            await _reviewRepository.UpdateAsync(review, cancellationToken);

            return new ReviewOperationResult(true, review.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReviewOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// VOTE ON REVIEW
// ============================================
public record VoteOnReviewCommand(Guid ReviewId, Guid CustomerId, bool IsHelpful) : IRequest<ReviewOperationResult>;

public class VoteOnReviewCommandHandler : IRequestHandler<VoteOnReviewCommand, ReviewOperationResult>
{
    private readonly IReviewRepository _reviewRepository;

    public VoteOnReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewOperationResult> Handle(VoteOnReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review == null)
                return new ReviewOperationResult(false, null, "Review not found");

            review.AddVote(request.CustomerId, request.IsHelpful);
            await _reviewRepository.UpdateAsync(review, cancellationToken);

            return new ReviewOperationResult(true, review.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReviewOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// ADD SELLER RESPONSE
// ============================================
public record AddSellerResponseCommand(
    Guid ReviewId,
    string Response,
    string ResponderName) : IRequest<ReviewOperationResult>;

public class AddSellerResponseCommandHandler : IRequestHandler<AddSellerResponseCommand, ReviewOperationResult>
{
    private readonly IReviewRepository _reviewRepository;

    public AddSellerResponseCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewOperationResult> Handle(AddSellerResponseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review == null)
                return new ReviewOperationResult(false, null, "Review not found");

            review.AddSellerResponse(request.Response, request.ResponderName);
            await _reviewRepository.UpdateAsync(review, cancellationToken);

            return new ReviewOperationResult(true, review.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReviewOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// APPROVE REVIEW
// ============================================
public record ApproveReviewCommand(Guid ReviewId, string? Notes = null) : IRequest<ReviewOperationResult>;

public class ApproveReviewCommandHandler : IRequestHandler<ApproveReviewCommand, ReviewOperationResult>
{
    private readonly IReviewRepository _reviewRepository;

    public ApproveReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewOperationResult> Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review == null)
                return new ReviewOperationResult(false, null, "Review not found");

            review.Approve(request.Notes);
            await _reviewRepository.UpdateAsync(review, cancellationToken);

            return new ReviewOperationResult(true, review.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReviewOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// REJECT REVIEW
// ============================================
public record RejectReviewCommand(Guid ReviewId, string Reason) : IRequest<ReviewOperationResult>;

public class RejectReviewCommandHandler : IRequestHandler<RejectReviewCommand, ReviewOperationResult>
{
    private readonly IReviewRepository _reviewRepository;

    public RejectReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewOperationResult> Handle(RejectReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review == null)
                return new ReviewOperationResult(false, null, "Review not found");

            review.Reject(request.Reason);
            await _reviewRepository.UpdateAsync(review, cancellationToken);

            return new ReviewOperationResult(true, review.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReviewOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// FLAG REVIEW
// ============================================
public record FlagReviewCommand(Guid ReviewId, string Reason) : IRequest<ReviewOperationResult>;

public class FlagReviewCommandHandler : IRequestHandler<FlagReviewCommand, ReviewOperationResult>
{
    private readonly IReviewRepository _reviewRepository;

    public FlagReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewOperationResult> Handle(FlagReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review == null)
                return new ReviewOperationResult(false, null, "Review not found");

            review.Flag(request.Reason);
            await _reviewRepository.UpdateAsync(review, cancellationToken);

            return new ReviewOperationResult(true, review.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReviewOperationResult(false, null, ex.Message);
        }
    }
}
