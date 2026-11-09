using EShop.Domain.Aggregates.WishlistAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Wishlists;

// Get Wishlist by ID
public record GetWishlistQuery(Guid WishlistId) : IRequest<WishlistDto?>;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistDto?>
{
    private readonly IWishlistRepository _repository;

    public GetWishlistQueryHandler(IWishlistRepository repository) => _repository = repository;

    public async Task<WishlistDto?> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var wishlist = await _repository.GetByIdAsync(request.WishlistId, cancellationToken);
        return wishlist != null ? WishlistDto.FromWishlist(wishlist) : null;
    }
}

// Get Customer Wishlists
public record GetCustomerWishlistsQuery(Guid CustomerId) : IRequest<IReadOnlyList<WishlistDto>>;

public class GetCustomerWishlistsQueryHandler : IRequestHandler<GetCustomerWishlistsQuery, IReadOnlyList<WishlistDto>>
{
    private readonly IWishlistRepository _repository;

    public GetCustomerWishlistsQueryHandler(IWishlistRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<WishlistDto>> Handle(GetCustomerWishlistsQuery request, CancellationToken cancellationToken)
    {
        var wishlists = await _repository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        return wishlists.Select(WishlistDto.FromWishlist).ToList();
    }
}

// Get Shared Wishlist
public record GetSharedWishlistQuery(string ShareToken) : IRequest<WishlistDto?>;

public class GetSharedWishlistQueryHandler : IRequestHandler<GetSharedWishlistQuery, WishlistDto?>
{
    private readonly IWishlistRepository _repository;

    public GetSharedWishlistQueryHandler(IWishlistRepository repository) => _repository = repository;

    public async Task<WishlistDto?> Handle(GetSharedWishlistQuery request, CancellationToken cancellationToken)
    {
        var wishlist = await _repository.GetByShareTokenAsync(request.ShareToken, cancellationToken);
        return wishlist != null ? WishlistDto.FromWishlist(wishlist) : null;
    }
}

// Check if Product in Wishlist
public record IsProductInWishlistQuery(Guid CustomerId, Guid ProductId) : IRequest<bool>;

public class IsProductInWishlistQueryHandler : IRequestHandler<IsProductInWishlistQuery, bool>
{
    private readonly IWishlistRepository _repository;

    public IsProductInWishlistQueryHandler(IWishlistRepository repository) => _repository = repository;

    public async Task<bool> Handle(IsProductInWishlistQuery request, CancellationToken cancellationToken)
    {
        return await _repository.IsProductInWishlistAsync(request.CustomerId, request.ProductId, cancellationToken);
    }
}

// DTOs
public record WishlistDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string? Description,
    WishlistType Type,
    WishlistPrivacy Privacy,
    bool IsDefault,
    string? ShareToken,
    int ItemCount,
    IReadOnlyList<WishlistItemDto> Items,
    DateTime CreatedAt,
    DateTime LastModifiedAt)
{
    public static WishlistDto FromWishlist(Wishlist w) => new(
        w.Id,
        w.CustomerId,
        w.Name,
        w.Description,
        w.Type,
        w.Privacy,
        w.IsDefault,
        w.ShareToken,
        w.ItemCount,
        w.Items.Select(i => new WishlistItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.PriceWhenAdded.Amount,
            i.CurrentPrice.Amount,
            i.CurrentPrice.Currency,
            i.ImageUrl,
            i.DesiredQuantity,
            i.Notes,
            i.Priority,
            i.IsPurchased,
            i.HasPriceDrop,
            i.PriceDropPercentage,
            i.AddedAt)).ToList(),
        w.CreatedAt,
        w.LastModifiedAt);
}

public record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal PriceWhenAdded,
    decimal CurrentPrice,
    string Currency,
    string? ImageUrl,
    int DesiredQuantity,
    string? Notes,
    int Priority,
    bool IsPurchased,
    bool HasPriceDrop,
    decimal PriceDropPercentage,
    DateTime AddedAt);
