using EShop.Domain.Aggregates.BundleAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Bundles;

// Create Fixed Bundle
public record CreateFixedBundleCommand(
    string Sku,
    string Name,
    string Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? ImageUrl = null) : IRequest<Guid>;

public class CreateFixedBundleCommandHandler : IRequestHandler<CreateFixedBundleCommand, Guid>
{
    private readonly IBundleRepository _repository;

    public CreateFixedBundleCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateFixedBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = Bundle.CreateFixed(
            request.Sku,
            request.Name,
            request.Description,
            request.DiscountType,
            request.DiscountValue,
            request.ImageUrl);

        await _repository.AddAsync(bundle, cancellationToken);
        return bundle.Id;
    }
}

// Create Customizable Bundle
public record CreateCustomizableBundleCommand(
    string Sku,
    string Name,
    string Description,
    int MinItems,
    int MaxItems,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? ImageUrl = null) : IRequest<Guid>;

public class CreateCustomizableBundleCommandHandler : IRequestHandler<CreateCustomizableBundleCommand, Guid>
{
    private readonly IBundleRepository _repository;

    public CreateCustomizableBundleCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateCustomizableBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = Bundle.CreateCustomizable(
            request.Sku,
            request.Name,
            request.Description,
            request.MinItems,
            request.MaxItems,
            request.DiscountType,
            request.DiscountValue,
            request.ImageUrl);

        await _repository.AddAsync(bundle, cancellationToken);
        return bundle.Id;
    }
}

// Create Mix & Match Bundle
public record CreateMixMatchBundleCommand(
    string Sku,
    string Name,
    string Description,
    int RequiredCount,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? ImageUrl = null) : IRequest<Guid>;

public class CreateMixMatchBundleCommandHandler : IRequestHandler<CreateMixMatchBundleCommand, Guid>
{
    private readonly IBundleRepository _repository;

    public CreateMixMatchBundleCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateMixMatchBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = Bundle.CreateMixAndMatch(
            request.Sku,
            request.Name,
            request.Description,
            request.RequiredCount,
            request.DiscountType,
            request.DiscountValue,
            request.ImageUrl);

        await _repository.AddAsync(bundle, cancellationToken);
        return bundle.Id;
    }
}

// Add Item to Bundle
public record AddBundleItemCommand(
    Guid BundleId,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    decimal UnitPrice,
    string Currency,
    int Quantity = 1,
    bool IsRequired = true,
    int? MinQuantity = null,
    int? MaxQuantity = null) : IRequest<Guid>;

public class AddBundleItemCommandHandler : IRequestHandler<AddBundleItemCommand, Guid>
{
    private readonly IBundleRepository _repository;

    public AddBundleItemCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddBundleItemCommand request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        var item = bundle.AddItem(
            request.ProductId,
            request.ProductName,
            request.ProductSku,
            new Money(request.UnitPrice, request.Currency),
            request.Quantity,
            request.IsRequired,
            request.MinQuantity,
            request.MaxQuantity);

        await _repository.UpdateAsync(bundle, cancellationToken);
        return item.Id;
    }
}

// Remove Item from Bundle
public record RemoveBundleItemCommand(Guid BundleId, Guid ItemId) : IRequest;

public class RemoveBundleItemCommandHandler : IRequestHandler<RemoveBundleItemCommand>
{
    private readonly IBundleRepository _repository;

    public RemoveBundleItemCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task Handle(RemoveBundleItemCommand request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        bundle.RemoveItem(request.ItemId);
        await _repository.UpdateAsync(bundle, cancellationToken);
    }
}

// Activate Bundle
public record ActivateBundleCommand(Guid BundleId) : IRequest;

public class ActivateBundleCommandHandler : IRequestHandler<ActivateBundleCommand>
{
    private readonly IBundleRepository _repository;

    public ActivateBundleCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task Handle(ActivateBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        bundle.Activate();
        await _repository.UpdateAsync(bundle, cancellationToken);
    }
}

// Deactivate Bundle
public record DeactivateBundleCommand(Guid BundleId) : IRequest;

public class DeactivateBundleCommandHandler : IRequestHandler<DeactivateBundleCommand>
{
    private readonly IBundleRepository _repository;

    public DeactivateBundleCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task Handle(DeactivateBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        bundle.Deactivate();
        await _repository.UpdateAsync(bundle, cancellationToken);
    }
}

// Update Bundle Discount
public record UpdateBundleDiscountCommand(Guid BundleId, DiscountType Type, decimal Value) : IRequest;

public class UpdateBundleDiscountCommandHandler : IRequestHandler<UpdateBundleDiscountCommand>
{
    private readonly IBundleRepository _repository;

    public UpdateBundleDiscountCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task Handle(UpdateBundleDiscountCommand request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        bundle.UpdateDiscount(request.Type, request.Value);
        await _repository.UpdateAsync(bundle, cancellationToken);
    }
}

// Set Validity Period
public record SetBundleValidityCommand(Guid BundleId, DateTime? ValidFrom, DateTime? ValidUntil) : IRequest;

public class SetBundleValidityCommandHandler : IRequestHandler<SetBundleValidityCommand>
{
    private readonly IBundleRepository _repository;

    public SetBundleValidityCommandHandler(IBundleRepository repository) => _repository = repository;

    public async Task Handle(SetBundleValidityCommand request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        bundle.SetValidityPeriod(request.ValidFrom, request.ValidUntil);
        await _repository.UpdateAsync(bundle, cancellationToken);
    }
}
