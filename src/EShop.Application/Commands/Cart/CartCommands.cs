using EShop.Domain.Aggregates.CartAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Cart;

// ============================================
// ADD ITEM TO CART
// ============================================
public record AddToCartCommand(
    Guid? CustomerId,
    string SessionId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl = null,
    Dictionary<string, string>? Attributes = null) : IRequest<CartOperationResult>;

public record CartOperationResult(
    bool Success,
    Guid CartId,
    int ItemCount,
    decimal Subtotal,
    string? ErrorMessage);

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartOperationResult>
{
    private readonly ICartRepository _cartRepository;

    public AddToCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartOperationResult> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get or create cart
            var cart = await _cartRepository.GetActiveCartAsync(request.CustomerId, request.SessionId, cancellationToken);
            
            if (cart == null)
            {
                cart = request.CustomerId.HasValue
                    ? ShoppingCart.CreateForCustomer(request.CustomerId.Value, request.SessionId)
                    : ShoppingCart.CreateForGuest(request.SessionId);
            }

            var unitPrice = new Money(request.UnitPrice, "KWD");
            cart.AddItem(
                request.ProductId,
                request.ProductName,
                null,
                unitPrice,
                request.Quantity,
                request.ImageUrl,
                request.Attributes);

            if (cart.Id == Guid.Empty || await _cartRepository.GetByIdAsync(cart.Id, cancellationToken) == null)
                await _cartRepository.AddAsync(cart, cancellationToken);
            else
                await _cartRepository.UpdateAsync(cart, cancellationToken);

            return new CartOperationResult(true, cart.Id, cart.ItemCount, cart.Subtotal.Amount, null);
        }
        catch (Exception ex)
        {
            return new CartOperationResult(false, Guid.Empty, 0, 0, ex.Message);
        }
    }
}

// ============================================
// UPDATE CART ITEM QUANTITY
// ============================================
public record UpdateCartItemCommand(
    Guid CartId,
    Guid ItemId,
    int NewQuantity) : IRequest<CartOperationResult>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartOperationResult>
{
    private readonly ICartRepository _cartRepository;

    public UpdateCartItemCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartOperationResult> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
            if (cart == null)
                return new CartOperationResult(false, Guid.Empty, 0, 0, "Cart not found");

            cart.UpdateItemQuantity(request.ItemId, request.NewQuantity);
            await _cartRepository.UpdateAsync(cart, cancellationToken);

            return new CartOperationResult(true, cart.Id, cart.ItemCount, cart.Subtotal.Amount, null);
        }
        catch (Exception ex)
        {
            return new CartOperationResult(false, Guid.Empty, 0, 0, ex.Message);
        }
    }
}

// ============================================
// REMOVE CART ITEM
// ============================================
public record RemoveCartItemCommand(Guid CartId, Guid ItemId) : IRequest<CartOperationResult>;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, CartOperationResult>
{
    private readonly ICartRepository _cartRepository;

    public RemoveCartItemCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartOperationResult> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
            if (cart == null)
                return new CartOperationResult(false, Guid.Empty, 0, 0, "Cart not found");

            cart.RemoveItem(request.ItemId);
            await _cartRepository.UpdateAsync(cart, cancellationToken);

            return new CartOperationResult(true, cart.Id, cart.ItemCount, cart.Subtotal.Amount, null);
        }
        catch (Exception ex)
        {
            return new CartOperationResult(false, Guid.Empty, 0, 0, ex.Message);
        }
    }
}

// ============================================
// APPLY PROMO CODE
// ============================================
public record ApplyPromoCodeCommand(
    Guid CartId,
    string PromoCode) : IRequest<ApplyPromoResult>;

public record ApplyPromoResult(
    bool Success,
    decimal DiscountAmount,
    decimal NewTotal,
    string? ErrorMessage);

public class ApplyPromoCodeCommandHandler : IRequestHandler<ApplyPromoCodeCommand, ApplyPromoResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly IPromotionRepository _promotionRepository;

    public ApplyPromoCodeCommandHandler(
        ICartRepository cartRepository,
        IPromotionRepository promotionRepository)
    {
        _cartRepository = cartRepository;
        _promotionRepository = promotionRepository;
    }

    public async Task<ApplyPromoResult> Handle(ApplyPromoCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
            if (cart == null)
                return new ApplyPromoResult(false, 0, 0, "Cart not found");

            var promotion = await _promotionRepository.GetByCodeAsync(request.PromoCode, cancellationToken);
            if (promotion == null)
                return new ApplyPromoResult(false, 0, 0, "Invalid promo code");

            // Validate promotion
            var productIds = cart.Items.Select(i => i.ProductId);
            var validation = promotion.Validate(
                cart.CustomerId ?? Guid.Empty,
                cart.Subtotal.Amount,
                1, // Default tier
                productIds);

            if (!validation.IsValid)
                return new ApplyPromoResult(false, 0, 0, validation.ErrorMessage);

            var discount = promotion.CalculateDiscount(cart.Subtotal.Amount, cart.Currency);
            cart.ApplyPromoCode(promotion.Id, request.PromoCode, discount);

            await _cartRepository.UpdateAsync(cart, cancellationToken);

            return new ApplyPromoResult(true, discount.Amount, cart.Total.Amount, null);
        }
        catch (Exception ex)
        {
            return new ApplyPromoResult(false, 0, 0, ex.Message);
        }
    }
}

// ============================================
// START CHECKOUT
// ============================================
public record StartCheckoutCommand(Guid CartId) : IRequest<CheckoutResult>;

public record CheckoutResult(
    bool Success,
    Guid? CartId,
    decimal Total,
    string? ErrorMessage);

public class StartCheckoutCommandHandler : IRequestHandler<StartCheckoutCommand, CheckoutResult>
{
    private readonly ICartRepository _cartRepository;

    public StartCheckoutCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CheckoutResult> Handle(StartCheckoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
            if (cart == null)
                return new CheckoutResult(false, null, 0, "Cart not found");

            cart.StartCheckout();
            await _cartRepository.UpdateAsync(cart, cancellationToken);

            return new CheckoutResult(true, cart.Id, cart.Total.Amount, null);
        }
        catch (Exception ex)
        {
            return new CheckoutResult(false, null, 0, ex.Message);
        }
    }
}
