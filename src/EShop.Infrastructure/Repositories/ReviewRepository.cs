using EShop.Domain.Aggregates.ReviewAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly EShopDbContext _context;

    public ReviewRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .Include(r => r.Images)
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ProductReview?> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .Where(r => r.ProductId == productId)
            .Include(r => r.Images)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetApprovedByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .Include(r => r.Images)
            .OrderByDescending(r => r.HelpfulVotes)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .Where(r => r.Status == ReviewStatus.PendingModeration)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetFlaggedReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .Where(r => r.Status == ReviewStatus.Flagged)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        await _context.Set<ProductReview>().AddAsync(review, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        _context.Set<ProductReview>().Update(review);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<ProductReview>().FindAsync(new object[] { id }, cancellationToken);
        if (review != null)
        {
            _context.Set<ProductReview>().Remove(review);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Set<ProductReview>()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .ToListAsync(cancellationToken);
            
        return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
    }

    public async Task<int> GetReviewCountAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .CountAsync(r => r.ProductId == productId && r.Status == ReviewStatus.Approved, cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Set<ProductReview>()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(1, 5)
            .ToDictionary(
                rating => rating,
                rating => reviews.Count(r => r.Rating == rating));
    }

    public async Task<bool> HasCustomerReviewedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductReview>()
            .AnyAsync(r => r.CustomerId == customerId && r.ProductId == productId, cancellationToken);
    }
}
