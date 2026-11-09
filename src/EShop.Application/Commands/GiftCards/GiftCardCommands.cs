using EShop.Domain.Aggregates.GiftCardAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.GiftCards;

// Create Digital Gift Card
public record CreateDigitalGiftCardCommand(
    decimal Amount,
    string Currency,
    Guid PurchasedByCustomerId,
    string RecipientEmail,
    string RecipientName,
    string? PersonalMessage = null,
    DateTime? ScheduledDelivery = null,
    int ExpiryYears = 2) : IRequest<Guid>;

public class CreateDigitalGiftCardCommandHandler : IRequestHandler<CreateDigitalGiftCardCommand, Guid>
{
    private readonly IGiftCardRepository _repository;

    public CreateDigitalGiftCardCommandHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateDigitalGiftCardCommand request, CancellationToken cancellationToken)
    {
        var giftCard = GiftCard.CreateDigital(
            request.Amount,
            request.Currency,
            request.PurchasedByCustomerId,
            request.RecipientEmail,
            request.RecipientName,
            request.PersonalMessage,
            request.ScheduledDelivery,
            request.ExpiryYears);

        await _repository.AddAsync(giftCard, cancellationToken);
        return giftCard.Id;
    }
}

// Create Physical Gift Card
public record CreatePhysicalGiftCardCommand(
    decimal Amount,
    string Currency,
    Guid? PurchasedByCustomerId = null,
    int ExpiryYears = 3) : IRequest<Guid>;

public class CreatePhysicalGiftCardCommandHandler : IRequestHandler<CreatePhysicalGiftCardCommand, Guid>
{
    private readonly IGiftCardRepository _repository;

    public CreatePhysicalGiftCardCommandHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreatePhysicalGiftCardCommand request, CancellationToken cancellationToken)
    {
        var giftCard = GiftCard.CreatePhysical(
            request.Amount,
            request.Currency,
            request.PurchasedByCustomerId,
            request.ExpiryYears);

        await _repository.AddAsync(giftCard, cancellationToken);
        return giftCard.Id;
    }
}

// Create Store Credit
public record CreateStoreCreditCommand(
    decimal Amount,
    string Currency,
    Guid CustomerId,
    string Reason,
    bool IsReloadable = true) : IRequest<Guid>;

public class CreateStoreCreditCommandHandler : IRequestHandler<CreateStoreCreditCommand, Guid>
{
    private readonly IGiftCardRepository _repository;

    public CreateStoreCreditCommandHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateStoreCreditCommand request, CancellationToken cancellationToken)
    {
        var giftCard = GiftCard.CreateStoreCredit(
            request.Amount,
            request.Currency,
            request.CustomerId,
            request.Reason,
            request.IsReloadable);

        await _repository.AddAsync(giftCard, cancellationToken);
        return giftCard.Id;
    }
}

// Activate Gift Card
public record ActivateGiftCardCommand(Guid GiftCardId) : IRequest;

public class ActivateGiftCardCommandHandler : IRequestHandler<ActivateGiftCardCommand>
{
    private readonly IGiftCardRepository _repository;

    public ActivateGiftCardCommandHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task Handle(ActivateGiftCardCommand request, CancellationToken cancellationToken)
    {
        var giftCard = await _repository.GetByIdAsync(request.GiftCardId, cancellationToken)
            ?? throw new InvalidOperationException("Gift card not found");

        giftCard.Activate();
        await _repository.UpdateAsync(giftCard, cancellationToken);
    }
}

// Redeem Gift Card
public record RedeemGiftCardCommand(
    string Code,
    decimal Amount,
    string Currency,
    Guid OrderId,
    string OrderNumber) : IRequest<decimal>;

public class RedeemGiftCardCommandHandler : IRequestHandler<RedeemGiftCardCommand, decimal>
{
    private readonly IGiftCardRepository _repository;

    public RedeemGiftCardCommandHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<decimal> Handle(RedeemGiftCardCommand request, CancellationToken cancellationToken)
    {
        var giftCard = await _repository.GetByCodeAsync(request.Code, cancellationToken)
            ?? throw new InvalidOperationException("Gift card not found");

        var redeemedAmount = giftCard.Redeem(
            new Money(request.Amount, request.Currency),
            request.OrderId,
            request.OrderNumber);

        await _repository.UpdateAsync(giftCard, cancellationToken);
        return redeemedAmount.Amount;
    }
}

// Reload Gift Card
public record ReloadGiftCardCommand(Guid GiftCardId, decimal Amount, string Currency) : IRequest;

public class ReloadGiftCardCommandHandler : IRequestHandler<ReloadGiftCardCommand>
{
    private readonly IGiftCardRepository _repository;

    public ReloadGiftCardCommandHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task Handle(ReloadGiftCardCommand request, CancellationToken cancellationToken)
    {
        var giftCard = await _repository.GetByIdAsync(request.GiftCardId, cancellationToken)
            ?? throw new InvalidOperationException("Gift card not found");

        giftCard.Reload(new Money(request.Amount, request.Currency));
        await _repository.UpdateAsync(giftCard, cancellationToken);
    }
}

// Suspend Gift Card
public record SuspendGiftCardCommand(Guid GiftCardId, string Reason) : IRequest;

public class SuspendGiftCardCommandHandler : IRequestHandler<SuspendGiftCardCommand>
{
    private readonly IGiftCardRepository _repository;

    public SuspendGiftCardCommandHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task Handle(SuspendGiftCardCommand request, CancellationToken cancellationToken)
    {
        var giftCard = await _repository.GetByIdAsync(request.GiftCardId, cancellationToken)
            ?? throw new InvalidOperationException("Gift card not found");

        giftCard.Suspend(request.Reason);
        await _repository.UpdateAsync(giftCard, cancellationToken);
    }
}
