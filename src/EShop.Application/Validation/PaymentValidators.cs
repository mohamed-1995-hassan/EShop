using FluentValidation;

namespace EShop.Application.Validation;

/// <summary>
/// Payment-related command validators
/// </summary>

public record ProcessPaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    PaymentDetailsDto PaymentDetails);

public record PaymentDetailsDto(
    string? CardNumber,
    string? CardHolderName,
    string? ExpiryMonth,
    string? ExpiryYear,
    string? Cvv,
    string? BankCode,
    string? AccountNumber);

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    private static readonly string[] ValidPaymentMethods = 
        { "CreditCard", "DebitCard", "BankTransfer", "Wallet", "COD" };

    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(100_000)
            .WithMessage("Amount must be between 0 and 100,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency code must be 3 characters");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => ValidPaymentMethods.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}");

        // Card validation when payment method is card
        When(x => x.PaymentMethod is "CreditCard" or "DebitCard", () =>
        {
            RuleFor(x => x.PaymentDetails.CardNumber)
                .NotEmpty()
                .CreditCard()
                .WithMessage("Valid card number is required");

            RuleFor(x => x.PaymentDetails.CardHolderName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Card holder name is required");

            RuleFor(x => x.PaymentDetails.ExpiryMonth)
                .NotEmpty()
                .Matches(@"^(0[1-9]|1[0-2])$")
                .WithMessage("Expiry month must be 01-12");

            RuleFor(x => x.PaymentDetails.ExpiryYear)
                .NotEmpty()
                .Matches(@"^\d{2}$")
                .Must(BeValidExpiryYear)
                .WithMessage("Expiry year must be valid (YY format)");

            RuleFor(x => x.PaymentDetails.Cvv)
                .NotEmpty()
                .Matches(@"^\d{3,4}$")
                .WithMessage("CVV must be 3 or 4 digits");
        });

        // Bank transfer validation
        When(x => x.PaymentMethod == "BankTransfer", () =>
        {
            RuleFor(x => x.PaymentDetails.BankCode)
                .NotEmpty()
                .WithMessage("Bank code is required for bank transfer");

            RuleFor(x => x.PaymentDetails.AccountNumber)
                .NotEmpty()
                .WithMessage("Account number is required for bank transfer");
        });
    }

    private bool BeValidExpiryYear(string? year)
    {
        if (string.IsNullOrEmpty(year) || !int.TryParse(year, out var yearInt))
            return false;

        var currentYear = DateTime.UtcNow.Year % 100;
        return yearInt >= currentYear && yearInt <= currentYear + 10;
    }
}

public record RefundPaymentCommand(
    Guid PaymentId,
    decimal Amount,
    string Reason);

public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Refund amount must be greater than zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(500)
            .WithMessage("Reason must be between 10 and 500 characters");
    }
}
