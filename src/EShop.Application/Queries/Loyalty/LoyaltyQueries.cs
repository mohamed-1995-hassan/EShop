using EShop.Domain.Aggregates.LoyaltyAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Loyalty;

// DTOs
public record LoyaltyAccountDto(
    Guid Id,
    Guid CustomerId,
    string MembershipNumber,
    LoyaltyTier Tier,
    int CurrentPoints,
    int LifetimePoints,
    int RedeemedPoints,
    int ExpiredPoints,
    decimal LifetimeSpend,
    decimal TierMultiplier,
    bool IsActive,
    DateTime EnrolledAt,
    DateTime? LastActivityAt,
    int PointsToNextTier,
    int ExpiringPointsNext30Days,
    IReadOnlyList<PointTransactionDto> RecentTransactions);

public record PointTransactionDto(
    Guid Id,
    int Points,
    PointTransactionType Type,
    string Description,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsExpired);

public record LoyaltySummaryDto(
    Guid Id,
    string MembershipNumber,
    LoyaltyTier Tier,
    int CurrentPoints,
    decimal TierMultiplier,
    bool IsActive);

// Get Account by ID
public record GetLoyaltyAccountQuery(Guid Id) : IRequest<LoyaltyAccountDto?>;

public class GetLoyaltyAccountQueryHandler : IRequestHandler<GetLoyaltyAccountQuery, LoyaltyAccountDto?>
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetLoyaltyAccountQueryHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<LoyaltyAccountDto?> Handle(GetLoyaltyAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return account == null ? null : MapToDto(account);
    }

    private static LoyaltyAccountDto MapToDto(LoyaltyAccount a)
    {
        var summary = a.GetSummary();
        return new LoyaltyAccountDto(
            a.Id,
            a.CustomerId,
            a.MembershipNumber,
            a.Tier,
            a.CurrentPoints,
            a.LifetimePoints,
            a.RedeemedPoints,
            a.ExpiredPoints,
            a.LifetimeSpend,
            a.TierMultiplier,
            a.IsActive,
            a.EnrolledAt,
            a.LastActivityAt,
            summary.PointsToNextTier,
            summary.ExpiringPointsNext30Days,
            a.Transactions.OrderByDescending(t => t.CreatedAt).Take(20)
                .Select(t => new PointTransactionDto(
                    t.Id, t.Points, t.Type, t.Description, t.CreatedAt, t.ExpiresAt, t.IsExpired))
                .ToList());
    }
}

// Get Account by Customer ID
public record GetLoyaltyAccountByCustomerQuery(Guid CustomerId) : IRequest<LoyaltyAccountDto?>;

public class GetLoyaltyAccountByCustomerQueryHandler : IRequestHandler<GetLoyaltyAccountByCustomerQuery, LoyaltyAccountDto?>
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetLoyaltyAccountByCustomerQueryHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<LoyaltyAccountDto?> Handle(GetLoyaltyAccountByCustomerQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (account == null) return null;

        var summary = account.GetSummary();
        return new LoyaltyAccountDto(
            account.Id,
            account.CustomerId,
            account.MembershipNumber,
            account.Tier,
            account.CurrentPoints,
            account.LifetimePoints,
            account.RedeemedPoints,
            account.ExpiredPoints,
            account.LifetimeSpend,
            account.TierMultiplier,
            account.IsActive,
            account.EnrolledAt,
            account.LastActivityAt,
            summary.PointsToNextTier,
            summary.ExpiringPointsNext30Days,
            account.Transactions.OrderByDescending(t => t.CreatedAt).Take(20)
                .Select(t => new PointTransactionDto(
                    t.Id, t.Points, t.Type, t.Description, t.CreatedAt, t.ExpiresAt, t.IsExpired))
                .ToList());
    }
}

// Get Accounts by Tier
public record GetAccountsByTierQuery(LoyaltyTier Tier) : IRequest<IReadOnlyList<LoyaltySummaryDto>>;

public class GetAccountsByTierQueryHandler : IRequestHandler<GetAccountsByTierQuery, IReadOnlyList<LoyaltySummaryDto>>
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetAccountsByTierQueryHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<LoyaltySummaryDto>> Handle(GetAccountsByTierQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetByTierAsync(request.Tier, cancellationToken);
        return accounts.Select(a => new LoyaltySummaryDto(
            a.Id, a.MembershipNumber, a.Tier, a.CurrentPoints, a.TierMultiplier, a.IsActive)).ToList();
    }
}

// Get Top Members
public record GetTopMembersQuery(int Count = 10) : IRequest<IReadOnlyList<LoyaltySummaryDto>>;

public class GetTopMembersQueryHandler : IRequestHandler<GetTopMembersQuery, IReadOnlyList<LoyaltySummaryDto>>
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetTopMembersQueryHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<LoyaltySummaryDto>> Handle(GetTopMembersQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetTopMembersAsync(request.Count, cancellationToken);
        return accounts.Select(a => new LoyaltySummaryDto(
            a.Id, a.MembershipNumber, a.Tier, a.CurrentPoints, a.TierMultiplier, a.IsActive)).ToList();
    }
}

// Get Accounts with Expiring Points
public record GetAccountsWithExpiringPointsQuery(int WithinDays = 30) : IRequest<IReadOnlyList<LoyaltySummaryDto>>;

public class GetAccountsWithExpiringPointsQueryHandler : IRequestHandler<GetAccountsWithExpiringPointsQuery, IReadOnlyList<LoyaltySummaryDto>>
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetAccountsWithExpiringPointsQueryHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<LoyaltySummaryDto>> Handle(GetAccountsWithExpiringPointsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetAccountsWithExpiringPointsAsync(request.WithinDays, cancellationToken);
        return accounts.Select(a => new LoyaltySummaryDto(
            a.Id, a.MembershipNumber, a.Tier, a.CurrentPoints, a.TierMultiplier, a.IsActive)).ToList();
    }
}

// Get Program Statistics
public record GetLoyaltyProgramStatsQuery : IRequest<LoyaltyProgramStatsDto>;

public record LoyaltyProgramStatsDto(int TotalActiveAccounts, long TotalOutstandingPoints);

public class GetLoyaltyProgramStatsQueryHandler : IRequestHandler<GetLoyaltyProgramStatsQuery, LoyaltyProgramStatsDto>
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetLoyaltyProgramStatsQueryHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<LoyaltyProgramStatsDto> Handle(GetLoyaltyProgramStatsQuery request, CancellationToken cancellationToken)
    {
        var totalAccounts = await _repository.GetTotalActiveAccountsAsync(cancellationToken);
        var totalPoints = await _repository.GetTotalOutstandingPointsAsync(cancellationToken);
        return new LoyaltyProgramStatsDto(totalAccounts, totalPoints);
    }
}
