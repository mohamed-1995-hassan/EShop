using FluentValidation;

namespace EShop.Application.Validation;

/// <summary>
/// Common validators reusable across different commands
/// </summary>

// ═══════════════════════════════════════════════════════════════════════════
// GENERIC PAGED QUERY VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public record PagedQuery(int PageNumber, int PageSize, string? SortBy, bool SortDescending);

public class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    private static readonly string[] AllowedSortFields = 
        { "id", "name", "createdAt", "updatedAt", "price", "date", "status" };

    public PagedQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SortBy)
            .Must(x => string.IsNullOrEmpty(x) || AllowedSortFields.Contains(x.ToLowerInvariant()))
            .WithMessage($"Sort field must be one of: {string.Join(", ", AllowedSortFields)}");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DATE RANGE VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public record DateRangeQuery(DateTime? StartDate, DateTime? EndDate);

public class DateRangeQueryValidator : AbstractValidator<DateRangeQuery>
{
    public DateRangeQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Start date must be before or equal to end date");

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date cannot be in the future");

        // Maximum date range of 1 year
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || 
                       (x.EndDate.Value - x.StartDate.Value).TotalDays <= 365)
            .WithMessage("Date range cannot exceed 1 year");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// EMAIL VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public static class EmailValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200)
            .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
            .WithMessage("A valid email address is required");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MONEY VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public record MoneyDto(decimal Amount, string Currency);

public class MoneyDtoValidator : AbstractValidator<MoneyDto>
{
    private static readonly string[] SupportedCurrencies = 
        { "KWD", "USD", "EUR", "GBP", "AED", "SAR", "BHD", "QAR", "OMR" };

    public MoneyDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10_000_000)
            .WithMessage("Amount must be between 0 and 10,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Must(c => SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PHONE NUMBER VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public static class PhoneValidatorExtensions
{
    public static IRuleBuilderOptions<T, string?> ValidPhoneNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x as string))
            .WithMessage("Phone number format is invalid");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// UUID VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public static class GuidValidatorExtensions
{
    public static IRuleBuilderOptions<T, Guid> ValidGuid<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        string fieldName = "ID")
    {
        return ruleBuilder
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .WithMessage($"{fieldName} is required and must be a valid GUID");
    }

    public static IRuleBuilderOptions<T, Guid?> ValidOptionalGuid<T>(
        this IRuleBuilder<T, Guid?> ruleBuilder,
        string fieldName = "ID")
    {
        return ruleBuilder
            .Must(x => !x.HasValue || x.Value != Guid.Empty)
            .WithMessage($"{fieldName} must be a valid GUID if provided");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SEARCH QUERY VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public record SearchQuery(string? SearchTerm, int? MaxResults);

public class SearchQueryValidator : AbstractValidator<SearchQuery>
{
    public SearchQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MinimumLength(2)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage("Search term must be between 2 and 100 characters");

        RuleFor(x => x.MaxResults)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .When(x => x.MaxResults.HasValue)
            .WithMessage("Max results must be between 1 and 1000");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// FILE UPLOAD VALIDATOR
// ═══════════════════════════════════════════════════════════════════════════

public record FileUploadDto(string FileName, string ContentType, long FileSize);

public class FileUploadDtoValidator : AbstractValidator<FileUploadDto>
{
    private static readonly string[] AllowedContentTypes = 
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static readonly string[] AllowedExtensions = 
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx" };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public FileUploadDtoValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(HaveValidExtension)
            .WithMessage($"File extension must be one of: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct.ToLowerInvariant()))
            .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must be less than {MaxFileSizeBytes / (1024 * 1024)}MB");
    }

    private static bool HaveValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }
}
