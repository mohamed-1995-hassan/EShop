using EShop.Domain.Aggregates.ReviewAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Reviews;

// ============================================
// GET REVIEW BY ID
// ============================================
public record GetReviewQuery(Guid ReviewId) : IRequest<ReviewDetailsDto?>;

public record ReviewDetailsDto(
    Guid Id,
    Guid ProductId,
    Guid CustomerId,
    string CustomerName,
    Guid? OrderId,
    int Rating,
    string Title,
    string Content,
    string Status,
    List<ReviewImageDto> Images,
    int HelpfulVotes,
    int UnhelpfulVotes,
    bool IsVerifiedPurchase,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ApprovedAt,
    List<ReviewResponseDto> Responses);

public record ReviewImageDto(
    Guid Id,
    string ImageUrl,
    string? Caption);

public record ReviewResponseDto(
    Guid Id,
    string Content,
    string ResponderName,
    string Type,
    DateTime CreatedAt);

public class GetReviewQueryHandler : IRequestHandler<GetReviewQuery, ReviewDetailsDto?>
{
    private readonly IReviewRepository _reviewRepository;

    public GetReviewQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ReviewDetailsDto?> Handle(GetReviewQuery request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review == null) return null;

        return MapToDetailsDto(review);
    }

    private static ReviewDetailsDto MapToDetailsDto(ProductReview review)
    {
        return new ReviewDetailsDto(
            review.Id,
            review.ProductId,
            review.CustomerId,
            review.CustomerName,
            review.OrderId,
            review.Rating,
            review.Title,
            review.Content,
            review.Status.ToString(),
            review.Images.Select(i => new ReviewImageDto(i.Id, i.ImageUrl, i.Caption)).ToList(),
            review.HelpfulVotes,
            review.UnhelpfulVotes,
            review.IsVerifiedPurchase,
            review.CreatedAt,
            review.UpdatedAt,
            review.ApprovedAt,
            review.Responses.Select(r => new ReviewResponseDto(
                r.Id,
                r.Content,
                r.ResponderName,
                r.Type.ToString(),
                r.CreatedAt)).ToList());
    }
}

// ============================================
// GET PRODUCT REVIEWS
// ============================================
public record GetProductReviewsQuery(
    Guid ProductId,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null) : IRequest<ProductReviewsResult>;

public record ProductReviewsResult(
    Guid ProductId,
    List<ReviewSummaryDto> Reviews,
    int TotalCount,
    int Page,
    int PageSize,
    double AverageRating,
    Dictionary<int, int> RatingDistribution);

public record ReviewSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    int Rating,
    string Title,
    string ContentPreview,
    int HelpfulVotes,
    bool IsVerifiedPurchase,
    DateTime CreatedAt,
    bool HasImages,
    bool HasResponses);

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, ProductReviewsResult>
{
    private readonly IReviewRepository _reviewRepository;

    public GetProductReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ProductReviewsResult> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetApprovedByProductIdAsync(request.ProductId, cancellationToken);
        var averageRating = await _reviewRepository.GetAverageRatingAsync(request.ProductId, cancellationToken);
        var ratingDistribution = await _reviewRepository.GetRatingDistributionAsync(request.ProductId, cancellationToken);

        var sortedReviews = request.SortBy?.ToLowerInvariant() switch
        {
            "rating_desc" => reviews.OrderByDescending(r => r.Rating),
            "rating_asc" => reviews.OrderBy(r => r.Rating),
            "helpful" => reviews.OrderByDescending(r => r.HelpfulVotes),
            "newest" => reviews.OrderByDescending(r => r.CreatedAt),
            _ => reviews.OrderByDescending(r => r.CreatedAt)
        };

        var totalCount = reviews.Count;
        var pagedReviews = sortedReviews
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReviewSummaryDto(
                r.Id,
                r.CustomerId,
                r.CustomerName,
                r.Rating,
                r.Title,
                r.Content.Length > 200 ? r.Content[..200] + "..." : r.Content,
                r.HelpfulVotes,
                r.IsVerifiedPurchase,
                r.CreatedAt,
                r.Images.Any(),
                r.Responses.Any()))
            .ToList();

        return new ProductReviewsResult(
            request.ProductId,
            pagedReviews,
            totalCount,
            request.Page,
            request.PageSize,
            averageRating,
            ratingDistribution);
    }
}

// ============================================
// GET CUSTOMER REVIEWS
// ============================================
public record GetCustomerReviewsQuery(Guid CustomerId) : IRequest<List<ReviewSummaryDto>>;

public class GetCustomerReviewsQueryHandler : IRequestHandler<GetCustomerReviewsQuery, List<ReviewSummaryDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetCustomerReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<List<ReviewSummaryDto>> Handle(GetCustomerReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return reviews.Select(r => new ReviewSummaryDto(
            r.Id,
            r.CustomerId,
            r.CustomerName,
            r.Rating,
            r.Title,
            r.Content.Length > 200 ? r.Content[..200] + "..." : r.Content,
            r.HelpfulVotes,
            r.IsVerifiedPurchase,
            r.CreatedAt,
            r.Images.Any(),
            r.Responses.Any())).ToList();
    }
}

// ============================================
// GET PENDING MODERATION
// ============================================
public record GetPendingModerationQuery() : IRequest<List<ReviewSummaryDto>>;

public class GetPendingModerationQueryHandler : IRequestHandler<GetPendingModerationQuery, List<ReviewSummaryDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetPendingModerationQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<List<ReviewSummaryDto>> Handle(GetPendingModerationQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetPendingModerationAsync(cancellationToken);

        return reviews.Select(r => new ReviewSummaryDto(
            r.Id,
            r.CustomerId,
            r.CustomerName,
            r.Rating,
            r.Title,
            r.Content.Length > 200 ? r.Content[..200] + "..." : r.Content,
            r.HelpfulVotes,
            r.IsVerifiedPurchase,
            r.CreatedAt,
            r.Images.Any(),
            r.Responses.Any())).ToList();
    }
}

// ============================================
// GET RATING SUMMARY
// ============================================
public record GetRatingSummaryQuery(Guid ProductId) : IRequest<RatingSummaryDto>;

public record RatingSummaryDto(
    Guid ProductId,
    double AverageRating,
    int TotalReviews,
    Dictionary<int, int> Distribution,
    int RecommendationPercentage);

public class GetRatingSummaryQueryHandler : IRequestHandler<GetRatingSummaryQuery, RatingSummaryDto>
{
    private readonly IReviewRepository _reviewRepository;

    public GetRatingSummaryQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<RatingSummaryDto> Handle(GetRatingSummaryQuery request, CancellationToken cancellationToken)
    {
        var averageRating = await _reviewRepository.GetAverageRatingAsync(request.ProductId, cancellationToken);
        var totalReviews = await _reviewRepository.GetReviewCountAsync(request.ProductId, cancellationToken);
        var distribution = await _reviewRepository.GetRatingDistributionAsync(request.ProductId, cancellationToken);

        // Calculate recommendation percentage (4+ stars = recommended)
        var recommendedCount = distribution.Where(d => d.Key >= 4).Sum(d => d.Value);
        var recommendationPercentage = totalReviews > 0 
            ? (int)Math.Round((double)recommendedCount / totalReviews * 100) 
            : 0;

        return new RatingSummaryDto(
            request.ProductId,
            averageRating,
            totalReviews,
            distribution,
            recommendationPercentage);
    }
}
