using EShop.Domain.Aggregates.SubscriptionAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Subscriptions;

// Create Subscription
public record CreateSubscriptionCommand(
    Guid CustomerId,
    SubscriptionType Type,
    BillingCycle BillingCycle,
    string Street,
    string City,
    string Country,
    string PostalCode,
    decimal DiscountPercentage = 10,
    int BillingDayOfMonth = 1) : IRequest<Guid>;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Guid>
{
    private readonly ISubscriptionRepository _repository;

    public CreateSubscriptionCommandHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var address = new Address(request.Street, request.City, request.Country, request.PostalCode);
        var subscription = Subscription.Create(
            request.CustomerId,
            request.Type,
            request.BillingCycle,
            address,
            request.DiscountPercentage,
            request.BillingDayOfMonth);

        await _repository.AddAsync(subscription, cancellationToken);
        return subscription.Id;
    }
}

// Add Item to Subscription
public record AddSubscriptionItemCommand(
    Guid SubscriptionId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    string? Variant = null) : IRequest<Guid>;

public class AddSubscriptionItemCommandHandler : IRequestHandler<AddSubscriptionItemCommand, Guid>
{
    private readonly ISubscriptionRepository _repository;

    public AddSubscriptionItemCommandHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddSubscriptionItemCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found");

        var item = subscription.AddItem(
            request.ProductId,
            request.ProductName,
            new Money(request.UnitPrice, request.Currency),
            request.Quantity,
            request.Variant);

        await _repository.UpdateAsync(subscription, cancellationToken);
        return item.Id;
    }
}

// Activate Subscription
public record ActivateSubscriptionCommand(Guid SubscriptionId, Guid PaymentMethodId) : IRequest;

public class ActivateSubscriptionCommandHandler : IRequestHandler<ActivateSubscriptionCommand>
{
    private readonly ISubscriptionRepository _repository;

    public ActivateSubscriptionCommandHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task Handle(ActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found");

        subscription.Activate(request.PaymentMethodId);
        await _repository.UpdateAsync(subscription, cancellationToken);
    }
}

// Pause Subscription
public record PauseSubscriptionCommand(Guid SubscriptionId, DateTime? Until = null, string? Reason = null) : IRequest;

public class PauseSubscriptionCommandHandler : IRequestHandler<PauseSubscriptionCommand>
{
    private readonly ISubscriptionRepository _repository;

    public PauseSubscriptionCommandHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task Handle(PauseSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found");

        subscription.Pause(request.Until, request.Reason);
        await _repository.UpdateAsync(subscription, cancellationToken);
    }
}

// Resume Subscription
public record ResumeSubscriptionCommand(Guid SubscriptionId) : IRequest;

public class ResumeSubscriptionCommandHandler : IRequestHandler<ResumeSubscriptionCommand>
{
    private readonly ISubscriptionRepository _repository;

    public ResumeSubscriptionCommandHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task Handle(ResumeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found");

        subscription.Resume();
        await _repository.UpdateAsync(subscription, cancellationToken);
    }
}

// Cancel Subscription
public record CancelSubscriptionCommand(Guid SubscriptionId, string Reason, bool Immediate = false) : IRequest;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand>
{
    private readonly ISubscriptionRepository _repository;

    public CancelSubscriptionCommandHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found");

        subscription.Cancel(request.Reason, request.Immediate);
        await _repository.UpdateAsync(subscription, cancellationToken);
    }
}

// Skip Next Delivery
public record SkipNextDeliveryCommand(Guid SubscriptionId, string? Reason = null) : IRequest;

public class SkipNextDeliveryCommandHandler : IRequestHandler<SkipNextDeliveryCommand>
{
    private readonly ISubscriptionRepository _repository;

    public SkipNextDeliveryCommandHandler(ISubscriptionRepository repository) => _repository = repository;

    public async Task Handle(SkipNextDeliveryCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found");

        subscription.SkipNextDelivery(request.Reason);
        await _repository.UpdateAsync(subscription, cancellationToken);
    }
}
