using EShop.Domain.Aggregates.GiftCardAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.GiftCards;

// Get Gift Card by ID
public record GetGiftCardQuery(Guid GiftCardId) : IRequest<GiftCardDto?>;

public class GetGiftCardQueryHandler : IRequestHandler<GetGiftCardQuery, GiftCardDto?>
{
    private readonly IGiftCardRepository _repository;

    public GetGiftCardQueryHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<GiftCardDto?> Handle(GetGiftCardQuery request, CancellationToken cancellationToken)
    {
        var giftCard = await _repository.GetByIdAsync(request.GiftCardId, cancellationToken);
        return giftCard != null ? GiftCardDto.FromGiftCard(giftCard) : null;
    }
}

// Check Gift Card Balance
public record CheckGiftCardBalanceQuery(string Code) : IRequest<GiftCardBalanceDto?>;

public class CheckGiftCardBalanceQueryHandler : IRequestHandler<CheckGiftCardBalanceQuery, GiftCardBalanceDto?>
{
    private readonly IGiftCardRepository _repository;

    public CheckGiftCardBalanceQueryHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<GiftCardBalanceDto?> Handle(CheckGiftCardBalanceQuery request, CancellationToken cancellationToken)
    {
        var giftCard = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (giftCard == null) return null;

        var info = giftCard.GetBalanceInfo();
        return new GiftCardBalanceDto(
            info.Code,
            info.CurrentBalance.Amount,
            info.InitialBalance.Amount,
            info.CurrentBalance.Currency,
            info.Status.ToString(),
            info.ExpiresAt,
            info.IsReloadable,
            giftCard.CanRedeem);
    }
}

// Get Customer Gift Cards (purchased by customer)
public record GetCustomerGiftCardsQuery(Guid CustomerId) : IRequest<IReadOnlyList<GiftCardDto>>;

public class GetCustomerGiftCardsQueryHandler : IRequestHandler<GetCustomerGiftCardsQuery, IReadOnlyList<GiftCardDto>>
{
    private readonly IGiftCardRepository _repository;

    public GetCustomerGiftCardsQueryHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<GiftCardDto>> Handle(GetCustomerGiftCardsQuery request, CancellationToken cancellationToken)
    {
        var purchased = await _repository.GetByPurchaserIdAsync(request.CustomerId, cancellationToken);
        var received = await _repository.GetByRecipientIdAsync(request.CustomerId, cancellationToken);
        var all = purchased.Concat(received).DistinctBy(g => g.Id).ToList();
        return all.Select(GiftCardDto.FromGiftCard).ToList();
    }
}

// Get Total Gift Card Liability (unredeemed balance)
public record GetGiftCardLiabilityQuery : IRequest<decimal>;

public class GetGiftCardLiabilityQueryHandler : IRequestHandler<GetGiftCardLiabilityQuery, decimal>
{
    private readonly IGiftCardRepository _repository;

    public GetGiftCardLiabilityQueryHandler(IGiftCardRepository repository) => _repository = repository;

    public async Task<decimal> Handle(GetGiftCardLiabilityQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetTotalUnredeemedBalanceAsync(cancellationToken);
    }
}

// DTOs
public record GiftCardDto(
    Guid Id,
    string Code,
    GiftCardType Type,
    GiftCardStatus Status,
    decimal InitialBalance,
    decimal CurrentBalance,
    string Currency,
    bool IsReloadable,
    DateTime? ExpiresAt,
    string? RecipientEmail,
    string? RecipientName,
    string? PersonalMessage,
    bool CanRedeem,
    DateTime CreatedAt)
{
    public static GiftCardDto FromGiftCard(GiftCard g) => new(
        g.Id,
        g.Code,
        g.Type,
        g.Status,
        g.InitialBalance.Amount,
        g.CurrentBalance.Amount,
        g.CurrentBalance.Currency,
        g.IsReloadable,
        g.ExpiresAt,
        g.RecipientEmail,
        g.RecipientName,
        g.PersonalMessage,
        g.CanRedeem,
        g.CreatedAt);
}

public record GiftCardBalanceDto(
    string Code,
    decimal CurrentBalance,
    decimal InitialBalance,
    string Currency,
    string Status,
    DateTime? ExpiresAt,
    bool IsReloadable,
    bool CanRedeem);
