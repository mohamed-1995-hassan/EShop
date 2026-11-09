using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Cart;

// ============================================
// GET CART
// ============================================
public record GetCartQuery(Guid? CustomerId, string SessionId) : IRequest<CartDto?>;

public record CartDto(
    Guid Id,
    Guid? CustomerId,
    string Status,
    List<CartItemDto> Items,
    List<SavedItemDto> SavedItems,
    decimal Subtotal,
    decimal Discount,
    decimal ShippingCost,
    decimal Tax,
    decimal Total,
    string? AppliedPromoCode,
    int? LoyaltyPointsToRedeem,
    int ItemCount,
    DateTime LastActivityAt);

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ImageUrl,
    Dictionary<string, string>? Attributes);

public record SavedItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string? ImageUrl,
    DateTime SavedAt);

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
{
    private readonly ICartRepository _cartRepository;

    public GetCartQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetActiveCartAsync(request.CustomerId, request.SessionId, cancellationToken);
        if (cart == null) return null;

        return new CartDto(
            cart.Id,
            cart.CustomerId,
            cart.Status.ToString(),
            cart.Items.Select(i => new CartItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Sku,
                i.UnitPrice.Amount,
                i.Quantity,
                i.LineTotal.Amount,
                i.ImageUrl,
                i.Attributes?.ToDictionary(a => a.Key, a => a.Value))).ToList(),
            cart.SavedItems.Select(s => new SavedItemDto(
                s.Id,
                s.ProductId,
                s.ProductName,
                s.UnitPrice.Amount,
                s.ImageUrl,
                s.SavedAt)).ToList(),
            cart.Subtotal.Amount,
            cart.Discount.Amount,
            cart.ShippingCost.Amount,
            cart.Tax.Amount,
            cart.Total.Amount,
            cart.AppliedPromoCode,
            cart.LoyaltyPointsToRedeem,
            cart.ItemCount,
            cart.LastActivityAt);
    }
}

// ============================================
// GET ABANDONED CARTS (Admin)
// ============================================
public record GetAbandonedCartsQuery(int HoursAbandoned = 24) : IRequest<List<AbandonedCartDto>>;

public record AbandonedCartDto(
    Guid CartId,
    Guid? CustomerId,
    string? CustomerEmail,
    decimal CartValue,
    int ItemCount,
    DateTime LastActivityAt,
    TimeSpan AbandonedDuration);

public class GetAbandonedCartsQueryHandler : IRequestHandler<GetAbandonedCartsQuery, List<AbandonedCartDto>>
{
    private readonly ICartRepository _cartRepository;

    public GetAbandonedCartsQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<List<AbandonedCartDto>> Handle(GetAbandonedCartsQuery request, CancellationToken cancellationToken)
    {
        var abandonedAfter = TimeSpan.FromHours(request.HoursAbandoned);
        var carts = await _cartRepository.GetAbandonedCartsAsync(abandonedAfter, cancellationToken);

        return carts.Select(c => new AbandonedCartDto(
            c.Id,
            c.CustomerId,
            null, // Would need to join with customer
            c.Subtotal.Amount,
            c.ItemCount,
            c.LastActivityAt,
            DateTime.UtcNow - c.LastActivityAt)).ToList();
    }
}
