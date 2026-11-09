using EShop.Domain.Aggregates.WishlistAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Wishlists;

// Create Wishlist
public record CreateWishlistCommand(
    Guid CustomerId,
    string Name,
    WishlistType Type,
    WishlistPrivacy Privacy = WishlistPrivacy.Private,
    string? Description = null) : IRequest<Guid>;

public class CreateWishlistCommandHandler : IRequestHandler<CreateWishlistCommand, Guid>
{
    private readonly IWishlistRepository _repository;

    public CreateWishlistCommandHandler(IWishlistRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateWishlistCommand request, CancellationToken cancellationToken)
    {
        var wishlist = Wishlist.Create(
            request.CustomerId,
            request.Name,
            request.Type,
            request.Privacy,
            request.Description);

        await _repository.AddAsync(wishlist, cancellationToken);
        return wishlist.Id;
    }
}

// Create Default Wishlist
public record CreateDefaultWishlistCommand(Guid CustomerId) : IRequest<Guid>;

public class CreateDefaultWishlistCommandHandler : IRequestHandler<CreateDefaultWishlistCommand, Guid>
{
    private readonly IWishlistRepository _repository;

    public CreateDefaultWishlistCommandHandler(IWishlistRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateDefaultWishlistCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetDefaultByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (existing != null)
            return existing.Id;

        var wishlist = Wishlist.CreateDefault(request.CustomerId);
        await _repository.AddAsync(wishlist, cancellationToken);
        return wishlist.Id;
    }
}

// Add Item to Wishlist
public record AddWishlistItemCommand(
    Guid WishlistId,
    Guid ProductId,
    string ProductName,
    decimal Price,
    string Currency,
    string? ImageUrl = null,
    int DesiredQuantity = 1,
    string? Notes = null) : IRequest<Guid>;

public class AddWishlistItemCommandHandler : IRequestHandler<AddWishlistItemCommand, Guid>
{
    private readonly IWishlistRepository _repository;

    public AddWishlistItemCommandHandler(IWishlistRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await _repository.GetByIdAsync(request.WishlistId, cancellationToken)
            ?? throw new InvalidOperationException("Wishlist not found");

        var item = wishlist.AddItem(
            request.ProductId,
            request.ProductName,
            new Money(request.Price, request.Currency),
            request.ImageUrl,
            request.DesiredQuantity,
            request.Notes);

        await _repository.UpdateAsync(wishlist, cancellationToken);
        return item.Id;
    }
}

// Remove Item from Wishlist
public record RemoveWishlistItemCommand(Guid WishlistId, Guid ItemId) : IRequest;

public class RemoveWishlistItemCommandHandler : IRequestHandler<RemoveWishlistItemCommand>
{
    private readonly IWishlistRepository _repository;

    public RemoveWishlistItemCommandHandler(IWishlistRepository repository) => _repository = repository;

    public async Task Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await _repository.GetByIdAsync(request.WishlistId, cancellationToken)
            ?? throw new InvalidOperationException("Wishlist not found");

        wishlist.RemoveItem(request.ItemId);
        await _repository.UpdateAsync(wishlist, cancellationToken);
    }
}

// Mark Item as Purchased
public record MarkWishlistItemPurchasedCommand(Guid WishlistId, Guid ItemId, Guid? PurchasedBy = null) : IRequest;

public class MarkWishlistItemPurchasedCommandHandler : IRequestHandler<MarkWishlistItemPurchasedCommand>
{
    private readonly IWishlistRepository _repository;

    public MarkWishlistItemPurchasedCommandHandler(IWishlistRepository repository) => _repository = repository;

    public async Task Handle(MarkWishlistItemPurchasedCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await _repository.GetByIdAsync(request.WishlistId, cancellationToken)
            ?? throw new InvalidOperationException("Wishlist not found");

        wishlist.MarkItemAsPurchased(request.ItemId, request.PurchasedBy);
        await _repository.UpdateAsync(wishlist, cancellationToken);
    }
}

// Update Wishlist Privacy
public record UpdateWishlistPrivacyCommand(Guid WishlistId, WishlistPrivacy Privacy) : IRequest;

public class UpdateWishlistPrivacyCommandHandler : IRequestHandler<UpdateWishlistPrivacyCommand>
{
    private readonly IWishlistRepository _repository;

    public UpdateWishlistPrivacyCommandHandler(IWishlistRepository repository) => _repository = repository;

    public async Task Handle(UpdateWishlistPrivacyCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await _repository.GetByIdAsync(request.WishlistId, cancellationToken)
            ?? throw new InvalidOperationException("Wishlist not found");

        wishlist.SetPrivacy(request.Privacy);
        await _repository.UpdateAsync(wishlist, cancellationToken);
    }
}
