using EShop.Application.Commands.Orders;
using FluentValidation;

namespace EShop.Application.Validation;

/// <summary>
/// Customer-related command validators
/// </summary>

public record CreateCustomerCommand(
    string Name,
    string Email,
    string? Phone,
    AddressDto? Address);

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .WithMessage("Name must be between 2 and 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200)
            .WithMessage("A valid email address is required");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number format is invalid");

        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!)
                .SetValidator(new AddressValidator());
        });
    }
}

public record UpdateCustomerCommand(
    Guid CustomerId,
    string Name,
    string? Phone,
    AddressDto? Address);

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .WithMessage("Name must be between 2 and 200 characters");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number format is invalid");

        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!)
                .SetValidator(new AddressValidator());
        });
    }
}
