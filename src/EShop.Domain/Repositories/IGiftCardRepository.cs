using EShop.Domain.Aggregates.GiftCardAggregate;

namespace EShop.Domain.Repositories;

public interface IGiftCardRepository
{
    Task<GiftCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GiftCard?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GiftCard>> GetByPurchaserIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GiftCard>> GetByRecipientIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GiftCard>> GetByStatusAsync(GiftCardStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GiftCard>> GetExpiringSoonAsync(int daysAhead, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GiftCard>> GetPendingDeliveryAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(GiftCard giftCard, CancellationToken cancellationToken = default);
    Task UpdateAsync(GiftCard giftCard, CancellationToken cancellationToken = default);
    
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    
    // Analytics
    Task<decimal> GetTotalUnredeemedBalanceAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRedeemedAmountAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
