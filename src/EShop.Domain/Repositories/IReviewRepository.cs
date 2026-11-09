using EShop.Domain.Aggregates.ReviewAggregate;

namespace EShop.Domain.Repositories;

public interface IReviewRepository
{
    Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductReview?> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetApprovedByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetPendingModerationAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetFlaggedReviewsAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(ProductReview review, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductReview review, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Statistics
    Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<int> GetReviewCountAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> HasCustomerReviewedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
}
