using EShop.Application.Commands.Orders;
using FluentValidation;

namespace EShop.Application.Validation;

/// <summary>
/// FLUENT VALIDATION - Order Commands
/// 
/// Benefits:
/// - Separates validation from command handlers
/// - Reusable validation rules
/// - Expressive, readable validation code
/// - Automatic error message generation
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .WithMessage("Shipping address is required");

        When(x => x.ShippingAddress != null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Street is required and must be less than 200 characters");

            RuleFor(x => x.ShippingAddress.City)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("City is required and must be less than 100 characters");

            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Country is required and must be less than 100 characters");

            RuleFor(x => x.ShippingAddress.PostalCode)
                .MaximumLength(20)
                .WithMessage("Postal code must be less than 20 characters");
        });

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required");

        RuleForEach(x => x.Items).SetValidator(new OrderItemDtoValidator());

        RuleFor(x => x.PromoCode)
            .MaximumLength(50)
            .When(x => x.PromoCode != null)
            .WithMessage("Promo code must be less than 50 characters");

        RuleFor(x => x.LoyaltyPointsToRedeem)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LoyaltyPointsToRedeem.HasValue)
            .WithMessage("Loyalty points cannot be negative");
    }
}

public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Product name is required and must be less than 200 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than zero");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Quantity must be between 1 and 100");
    }
}

public class AddressValidator : AbstractValidator<AddressDto>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Street is required");

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("City is required");

        RuleFor(x => x.Country)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Country is required");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("Postal code must be less than 20 characters");
    }
}
