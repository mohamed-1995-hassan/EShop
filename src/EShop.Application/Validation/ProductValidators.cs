using FluentValidation;

namespace EShop.Application.Validation;

/// <summary>
/// Product-related command validators
/// </summary>

public record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int InitialStock,
    int LowStockThreshold,
    List<string>? Categories);

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private static readonly string[] SupportedCurrencies = { "KWD", "USD", "EUR", "GBP", "AED", "SAR" };

    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Z0-9\-]+$")
            .WithMessage("SKU must be alphanumeric with hyphens only (uppercase)");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .WithMessage("Product name must be between 2 and 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description != null)
            .WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Price must be between 0 and 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Must(c => SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100_000)
            .WithMessage("Initial stock must be between 0 and 100,000");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0)
            .LessThan(x => x.InitialStock)
            .WithMessage("Low stock threshold must be less than initial stock");

        When(x => x.Categories != null && x.Categories.Any(), () =>
        {
            RuleFor(x => x.Categories!.Count)
                .LessThanOrEqualTo(10)
                .WithMessage("Maximum 10 categories allowed");

            RuleForEach(x => x.Categories)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Category name cannot be empty or exceed 100 characters");
        });
    }
}

public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    decimal Price,
    string Currency);

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private static readonly string[] SupportedCurrencies = { "KWD", "USD", "EUR", "GBP", "AED", "SAR" };

    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .WithMessage("Product name must be between 2 and 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description != null)
            .WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Price must be between 0 and 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Must(c => SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");
    }
}

public record UpdateStockCommand(
    Guid ProductId,
    int QuantityChange,
    string Reason);

public class UpdateStockCommandValidator : AbstractValidator<UpdateStockCommand>
{
    public UpdateStockCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.QuantityChange)
            .NotEqual(0)
            .WithMessage("Quantity change cannot be zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Reason is required and must be less than 500 characters");
    }
}
