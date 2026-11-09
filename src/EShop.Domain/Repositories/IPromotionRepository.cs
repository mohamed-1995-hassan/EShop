using EShop.Domain.Aggregates.PromotionAggregate;

namespace EShop.Domain.Repositories;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetByTypeAsync(PromotionType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetUpcomingPromotionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetExpiredPromotionsAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetPromotionsForProductAsync(Guid productId, CancellationToken cancellationToken = default);
    
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<int> GetCustomerUsageCountAsync(Guid promotionId, Guid customerId, CancellationToken cancellationToken = default);
}
