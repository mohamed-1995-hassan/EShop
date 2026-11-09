using EShop.Domain.Aggregates.SubscriptionAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Subscriptions;

// Get Subscription by ID
public record GetSubscriptionQuery(Guid SubscriptionId) : IRequest<SubscriptionDto?>;

public class GetSubscriptionQueryHandler : IRequestHandler<GetSubscriptionQuery, SubscriptionDto?>
{
    private readonly ISubscriptionRepository _repository;

    public GetSubscriptionQueryHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task<SubscriptionDto?> Handle(GetSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        return subscription != null ? SubscriptionDto.FromSubscription(subscription) : null;
    }
}

// Get Customer Subscriptions
public record GetCustomerSubscriptionsQuery(Guid CustomerId) : IRequest<IReadOnlyList<SubscriptionDto>>;

public class GetCustomerSubscriptionsQueryHandler : IRequestHandler<GetCustomerSubscriptionsQuery, IReadOnlyList<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _repository;

    public GetCustomerSubscriptionsQueryHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<SubscriptionDto>> Handle(GetCustomerSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subscriptions = await _repository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        return subscriptions.Select(SubscriptionDto.FromSubscription).ToList();
    }
}

// Get Subscriptions Due for Billing
public record GetSubscriptionsDueForBillingQuery(DateTime Date) : IRequest<IReadOnlyList<SubscriptionDto>>;

public class GetSubscriptionsDueForBillingQueryHandler : IRequestHandler<GetSubscriptionsDueForBillingQuery, IReadOnlyList<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _repository;

    public GetSubscriptionsDueForBillingQueryHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<SubscriptionDto>> Handle(GetSubscriptionsDueForBillingQuery request, CancellationToken cancellationToken)
    {
        var subscriptions = await _repository.GetDueForBillingAsync(request.Date, cancellationToken);
        return subscriptions.Select(SubscriptionDto.FromSubscription).ToList();
    }
}

// DTOs
public record SubscriptionDto(
    Guid Id,
    string SubscriptionNumber,
    Guid CustomerId,
    SubscriptionType Type,
    SubscriptionStatus Status,
    BillingCycle BillingCycle,
    decimal BasePrice,
    decimal DiscountAmount,
    decimal DiscountPercentage,
    decimal TotalPrice,
    string Currency,
    DateTime? NextBillingDate,
    DateTime? NextDeliveryDate,
    int TotalDeliveries,
    int SkippedDeliveries,
    bool CanSkipNextDelivery,
    IReadOnlyList<SubscriptionItemDto> Items,
    DateTime CreatedAt)
{
    public static SubscriptionDto FromSubscription(Subscription s) => new(
        s.Id,
        s.SubscriptionNumber,
        s.CustomerId,
        s.Type,
        s.Status,
        s.BillingCycle,
        s.BasePrice.Amount,
        s.DiscountAmount.Amount,
        s.DiscountPercentage,
        s.TotalPrice.Amount,
        s.TotalPrice.Currency,
        s.NextBillingDate,
        s.NextDeliveryDate,
        s.TotalDeliveries,
        s.SkippedDeliveries,
        s.CanSkipNextDelivery,
        s.Items.Select(i => new SubscriptionItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.UnitPrice.Amount,
            i.UnitPrice.Currency,
            i.Quantity,
            i.Variant)).ToList(),
        s.CreatedAt);
}

public record SubscriptionItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    string? Variant);
