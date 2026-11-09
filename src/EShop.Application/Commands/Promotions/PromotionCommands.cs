using EShop.Domain.Aggregates.PromotionAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Promotions;

// ============================================
// CREATE PERCENTAGE PROMOTION
// ============================================
public record CreatePercentagePromotionCommand(
    string Code,
    string Name,
    decimal Percentage,
    DateTime StartDate,
    DateTime EndDate,
    decimal? MaxDiscountAmount = null,
    decimal? MinimumOrderValue = null,
    int? MaxUsageTotal = null,
    int? MaxUsagePerCustomer = null) : IRequest<CreatePromotionResult>;

public record CreatePromotionResult(
    bool Success,
    Guid? PromotionId,
    string? Code,
    string? ErrorMessage);

public class CreatePercentagePromotionCommandHandler : IRequestHandler<CreatePercentagePromotionCommand, CreatePromotionResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public CreatePercentagePromotionCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<CreatePromotionResult> Handle(CreatePercentagePromotionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if code already exists
            var codeExists = await _promotionRepository.CodeExistsAsync(request.Code, cancellationToken);
            if (codeExists)
                return new CreatePromotionResult(false, null, null, "Promo code already exists");

            var promotion = Promotion.CreatePercentageDiscount(
                request.Code,
                request.Name,
                request.Percentage,
                request.StartDate,
                request.EndDate,
                request.MaxDiscountAmount,
                request.MinimumOrderValue);

            if (request.MaxUsageTotal.HasValue || request.MaxUsagePerCustomer.HasValue)
                promotion.SetUsageLimits(request.MaxUsageTotal, request.MaxUsagePerCustomer);

            await _promotionRepository.AddAsync(promotion, cancellationToken);

            return new CreatePromotionResult(true, promotion.Id, promotion.Code, null);
        }
        catch (Exception ex)
        {
            return new CreatePromotionResult(false, null, null, ex.Message);
        }
    }
}

// ============================================
// CREATE FIXED DISCOUNT PROMOTION
// ============================================
public record CreateFixedDiscountPromotionCommand(
    string Code,
    string Name,
    decimal Amount,
    string Currency,
    DateTime StartDate,
    DateTime EndDate,
    decimal? MinimumOrderValue = null,
    int? MaxUsageTotal = null,
    int? MaxUsagePerCustomer = null) : IRequest<CreatePromotionResult>;

public class CreateFixedDiscountPromotionCommandHandler : IRequestHandler<CreateFixedDiscountPromotionCommand, CreatePromotionResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public CreateFixedDiscountPromotionCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<CreatePromotionResult> Handle(CreateFixedDiscountPromotionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var codeExists = await _promotionRepository.CodeExistsAsync(request.Code, cancellationToken);
            if (codeExists)
                return new CreatePromotionResult(false, null, null, "Promo code already exists");

            var promotion = Promotion.CreateFixedDiscount(
                request.Code,
                request.Name,
                request.Amount,
                request.Currency,
                request.StartDate,
                request.EndDate,
                request.MinimumOrderValue);

            if (request.MaxUsageTotal.HasValue || request.MaxUsagePerCustomer.HasValue)
                promotion.SetUsageLimits(request.MaxUsageTotal, request.MaxUsagePerCustomer);

            await _promotionRepository.AddAsync(promotion, cancellationToken);

            return new CreatePromotionResult(true, promotion.Id, promotion.Code, null);
        }
        catch (Exception ex)
        {
            return new CreatePromotionResult(false, null, null, ex.Message);
        }
    }
}

// ============================================
// CREATE FREE SHIPPING PROMOTION
// ============================================
public record CreateFreeShippingPromotionCommand(
    string Code,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal? MinimumOrderValue = null) : IRequest<CreatePromotionResult>;

public class CreateFreeShippingPromotionCommandHandler : IRequestHandler<CreateFreeShippingPromotionCommand, CreatePromotionResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public CreateFreeShippingPromotionCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<CreatePromotionResult> Handle(CreateFreeShippingPromotionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var codeExists = await _promotionRepository.CodeExistsAsync(request.Code, cancellationToken);
            if (codeExists)
                return new CreatePromotionResult(false, null, null, "Promo code already exists");

            var promotion = Promotion.CreateFreeShipping(
                request.Code,
                request.Name,
                request.StartDate,
                request.EndDate,
                request.MinimumOrderValue);

            await _promotionRepository.AddAsync(promotion, cancellationToken);

            return new CreatePromotionResult(true, promotion.Id, promotion.Code, null);
        }
        catch (Exception ex)
        {
            return new CreatePromotionResult(false, null, null, ex.Message);
        }
    }
}

// ============================================
// ACTIVATE PROMOTION
// ============================================
public record ActivatePromotionCommand(Guid PromotionId) : IRequest<PromotionOperationResult>;

public record PromotionOperationResult(bool Success, bool? IsActive, string? ErrorMessage);

public class ActivatePromotionCommandHandler : IRequestHandler<ActivatePromotionCommand, PromotionOperationResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public ActivatePromotionCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromotionOperationResult> Handle(ActivatePromotionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
            if (promotion == null)
                return new PromotionOperationResult(false, null, "Promotion not found");

            promotion.Activate();
            await _promotionRepository.UpdateAsync(promotion, cancellationToken);

            return new PromotionOperationResult(true, promotion.IsActive, null);
        }
        catch (Exception ex)
        {
            return new PromotionOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// DEACTIVATE PROMOTION
// ============================================
public record DeactivatePromotionCommand(Guid PromotionId) : IRequest<PromotionOperationResult>;

public class DeactivatePromotionCommandHandler : IRequestHandler<DeactivatePromotionCommand, PromotionOperationResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public DeactivatePromotionCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromotionOperationResult> Handle(DeactivatePromotionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
            if (promotion == null)
                return new PromotionOperationResult(false, null, "Promotion not found");

            promotion.Deactivate();
            await _promotionRepository.UpdateAsync(promotion, cancellationToken);

            return new PromotionOperationResult(true, promotion.IsActive, null);
        }
        catch (Exception ex)
        {
            return new PromotionOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// EXTEND PROMOTION
// ============================================
public record ExtendPromotionCommand(Guid PromotionId, DateTime NewEndDate) : IRequest<PromotionOperationResult>;

public class ExtendPromotionCommandHandler : IRequestHandler<ExtendPromotionCommand, PromotionOperationResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public ExtendPromotionCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromotionOperationResult> Handle(ExtendPromotionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
            if (promotion == null)
                return new PromotionOperationResult(false, null, "Promotion not found");

            promotion.ExtendEndDate(request.NewEndDate);
            await _promotionRepository.UpdateAsync(promotion, cancellationToken);

            return new PromotionOperationResult(true, promotion.IsActive, null);
        }
        catch (Exception ex)
        {
            return new PromotionOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// VALIDATE PROMO CODE
// ============================================
public record ValidatePromoCodeCommand(
    string Code,
    Guid CustomerId,
    decimal OrderTotal,
    int CustomerTier = 1,
    List<Guid>? ProductIds = null) : IRequest<PromoValidationResult>;

public record PromoValidationResult(
    bool IsValid,
    Guid? PromotionId,
    decimal? DiscountAmount,
    string? DiscountType,
    string? ErrorMessage);

public class ValidatePromoCodeCommandHandler : IRequestHandler<ValidatePromoCodeCommand, PromoValidationResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public ValidatePromoCodeCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromoValidationResult> Handle(ValidatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promotion = await _promotionRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (promotion == null)
                return new PromoValidationResult(false, null, null, null, "Invalid promo code");

            var validation = promotion.Validate(
                request.CustomerId,
                request.OrderTotal,
                request.CustomerTier,
                request.ProductIds ?? new List<Guid>());

            if (!validation.IsValid)
                return new PromoValidationResult(false, null, null, null, validation.ErrorMessage);

            var discount = promotion.CalculateDiscount(request.OrderTotal, "KWD");

            return new PromoValidationResult(
                true,
                promotion.Id,
                discount.Amount,
                promotion.DiscountType.ToString(),
                null);
        }
        catch (Exception ex)
        {
            return new PromoValidationResult(false, null, null, null, ex.Message);
        }
    }
}

// ============================================
// RECORD PROMOTION USAGE
// ============================================
public record RecordPromotionUsageCommand(
    Guid PromotionId,
    Guid CustomerId,
    Guid OrderId,
    decimal DiscountApplied) : IRequest<PromotionOperationResult>;

public class RecordPromotionUsageCommandHandler : IRequestHandler<RecordPromotionUsageCommand, PromotionOperationResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public RecordPromotionUsageCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromotionOperationResult> Handle(RecordPromotionUsageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
            if (promotion == null)
                return new PromotionOperationResult(false, null, "Promotion not found");

            promotion.RecordUsage(request.CustomerId, request.OrderId, request.DiscountApplied);
            await _promotionRepository.UpdateAsync(promotion, cancellationToken);

            return new PromotionOperationResult(true, promotion.IsActive, null);
        }
        catch (Exception ex)
        {
            return new PromotionOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// ADD APPLICABLE PRODUCT
// ============================================
public record AddApplicableProductCommand(Guid PromotionId, Guid ProductId) : IRequest<PromotionOperationResult>;

public class AddApplicableProductCommandHandler : IRequestHandler<AddApplicableProductCommand, PromotionOperationResult>
{
    private readonly IPromotionRepository _promotionRepository;

    public AddApplicableProductCommandHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromotionOperationResult> Handle(AddApplicableProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
            if (promotion == null)
                return new PromotionOperationResult(false, null, "Promotion not found");

            promotion.AddApplicableProduct(request.ProductId);
            await _promotionRepository.UpdateAsync(promotion, cancellationToken);

            return new PromotionOperationResult(true, promotion.IsActive, null);
        }
        catch (Exception ex)
        {
            return new PromotionOperationResult(false, null, ex.Message);
        }
    }
}
