using EShop.Domain.Aggregates.LoyaltyAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Loyalty;

// Enroll Customer
public record EnrollCustomerCommand(Guid CustomerId, int WelcomeBonus = 100) : IRequest<Guid>;

public class EnrollCustomerCommandHandler : IRequestHandler<EnrollCustomerCommand, Guid>
{
    private readonly ILoyaltyAccountRepository _repository;

    public EnrollCustomerCommandHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<Guid> Handle(EnrollCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (existing != null)
            return existing.Id;

        var account = LoyaltyAccount.Enroll(request.CustomerId, request.WelcomeBonus);
        await _repository.AddAsync(account, cancellationToken);
        return account.Id;
    }
}

// Earn Points from Purchase
public record EarnPointsCommand(
    Guid AccountId,
    Guid OrderId,
    decimal OrderAmount,
    string Currency,
    decimal PointsPerCurrency = 1) : IRequest;

public class EarnPointsCommandHandler : IRequestHandler<EarnPointsCommand>
{
    private readonly ILoyaltyAccountRepository _repository;

    public EarnPointsCommandHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task Handle(EarnPointsCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new InvalidOperationException("Loyalty account not found");

        account.EarnPoints(request.OrderId, request.OrderAmount, request.Currency, request.PointsPerCurrency);
        await _repository.UpdateAsync(account, cancellationToken);
    }
}

// Earn Bonus Points
public record EarnBonusPointsCommand(
    Guid AccountId,
    int Points,
    string Reason,
    PointTransactionType Type = PointTransactionType.Bonus) : IRequest;

public class EarnBonusPointsCommandHandler : IRequestHandler<EarnBonusPointsCommand>
{
    private readonly ILoyaltyAccountRepository _repository;

    public EarnBonusPointsCommandHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task Handle(EarnBonusPointsCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new InvalidOperationException("Loyalty account not found");

        account.EarnBonusPoints(request.Points, request.Reason, request.Type);
        await _repository.UpdateAsync(account, cancellationToken);
    }
}

// Redeem Points for Discount
public record RedeemPointsForDiscountCommand(
    Guid AccountId,
    int Points,
    decimal DiscountAmount,
    string Currency,
    Guid OrderId) : IRequest<PointRedemption>;

public class RedeemPointsForDiscountCommandHandler : IRequestHandler<RedeemPointsForDiscountCommand, PointRedemption>
{
    private readonly ILoyaltyAccountRepository _repository;

    public RedeemPointsForDiscountCommandHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<PointRedemption> Handle(RedeemPointsForDiscountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new InvalidOperationException("Loyalty account not found");

        var redemption = account.RedeemForDiscount(
            request.Points,
            new Money(request.DiscountAmount, request.Currency),
            request.OrderId);

        await _repository.UpdateAsync(account, cancellationToken);
        return redemption;
    }
}

// Redeem Points for Reward
public record RedeemPointsForRewardCommand(
    Guid AccountId,
    Guid RewardId,
    string RewardName,
    int RequiredPoints) : IRequest<PointRedemption>;

public class RedeemPointsForRewardCommandHandler : IRequestHandler<RedeemPointsForRewardCommand, PointRedemption>
{
    private readonly ILoyaltyAccountRepository _repository;

    public RedeemPointsForRewardCommandHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task<PointRedemption> Handle(RedeemPointsForRewardCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new InvalidOperationException("Loyalty account not found");

        var redemption = account.RedeemForReward(request.RewardId, request.RewardName, request.RequiredPoints);
        await _repository.UpdateAsync(account, cancellationToken);
        return redemption;
    }
}

// Suspend Account
public record SuspendLoyaltyAccountCommand(Guid AccountId, string Reason) : IRequest;

public class SuspendLoyaltyAccountCommandHandler : IRequestHandler<SuspendLoyaltyAccountCommand>
{
    private readonly ILoyaltyAccountRepository _repository;

    public SuspendLoyaltyAccountCommandHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task Handle(SuspendLoyaltyAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new InvalidOperationException("Loyalty account not found");

        account.Suspend(request.Reason);
        await _repository.UpdateAsync(account, cancellationToken);
    }
}

// Reactivate Account
public record ReactivateLoyaltyAccountCommand(Guid AccountId) : IRequest;

public class ReactivateLoyaltyAccountCommandHandler : IRequestHandler<ReactivateLoyaltyAccountCommand>
{
    private readonly ILoyaltyAccountRepository _repository;

    public ReactivateLoyaltyAccountCommandHandler(ILoyaltyAccountRepository repository) => _repository = repository;

    public async Task Handle(ReactivateLoyaltyAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new InvalidOperationException("Loyalty account not found");

        account.Reactivate();
        await _repository.UpdateAsync(account, cancellationToken);
    }
}
