using EShop.Domain.Aggregates.PromotionAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Promotions;

// ============================================
// GET PROMOTION BY ID
// ============================================
public record GetPromotionQuery(Guid PromotionId) : IRequest<PromotionDetailsDto?>;

public record PromotionDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderValue,
    int? MaxUsageTotal,
    int? MaxUsagePerCustomer,
    int CurrentUsageCount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    bool IsStackable,
    string Scope,
    List<Guid> ApplicableProductIds,
    List<Guid> ApplicableCategoryIds,
    DateTime CreatedAt);

public class GetPromotionQueryHandler : IRequestHandler<GetPromotionQuery, PromotionDetailsDto?>
{
    private readonly IPromotionRepository _promotionRepository;

    public GetPromotionQueryHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromotionDetailsDto?> Handle(GetPromotionQuery request, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
        if (promotion == null) return null;

        return MapToDetailsDto(promotion);
    }

    private static PromotionDetailsDto MapToDetailsDto(Promotion promotion)
    {
        return new PromotionDetailsDto(
            promotion.Id,
            promotion.Code,
            promotion.Name,
            promotion.Description,
            promotion.Type.ToString(),
            promotion.DiscountType.ToString(),
            promotion.DiscountValue,
            promotion.MaxDiscountAmount,
            promotion.MinimumOrderValue,
            promotion.MaxUsageTotal,
            promotion.MaxUsagePerCustomer,
            promotion.CurrentUsageCount,
            promotion.StartDate,
            promotion.EndDate,
            promotion.IsActive,
            promotion.IsStackable,
            promotion.Scope.ToString(),
            promotion.ApplicableProductIds.ToList(),
            promotion.ApplicableCategoryIds.ToList(),
            promotion.CreatedAt);
    }
}

// ============================================
// GET ACTIVE PROMOTIONS
// ============================================
public record GetActivePromotionsQuery() : IRequest<List<PromotionSummaryDto>>;

public record PromotionSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive);

public class GetActivePromotionsQueryHandler : IRequestHandler<GetActivePromotionsQuery, List<PromotionSummaryDto>>
{
    private readonly IPromotionRepository _promotionRepository;

    public GetActivePromotionsQueryHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<List<PromotionSummaryDto>> Handle(GetActivePromotionsQuery request, CancellationToken cancellationToken)
    {
        var promotions = await _promotionRepository.GetActivePromotionsAsync(cancellationToken);

        return promotions.Select(p => new PromotionSummaryDto(
            p.Id,
            p.Code,
            p.Name,
            p.Type.ToString(),
            p.DiscountType.ToString(),
            p.DiscountValue,
            p.StartDate,
            p.EndDate,
            p.IsActive)).ToList();
    }
}

// ============================================
// GET PROMOTIONS BY TYPE
// ============================================
public record GetPromotionsByTypeQuery(PromotionType Type) : IRequest<List<PromotionSummaryDto>>;

public class GetPromotionsByTypeQueryHandler : IRequestHandler<GetPromotionsByTypeQuery, List<PromotionSummaryDto>>
{
    private readonly IPromotionRepository _promotionRepository;

    public GetPromotionsByTypeQueryHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<List<PromotionSummaryDto>> Handle(GetPromotionsByTypeQuery request, CancellationToken cancellationToken)
    {
        var promotions = await _promotionRepository.GetByTypeAsync(request.Type, cancellationToken);

        return promotions.Select(p => new PromotionSummaryDto(
            p.Id,
            p.Code,
            p.Name,
            p.Type.ToString(),
            p.DiscountType.ToString(),
            p.DiscountValue,
            p.StartDate,
            p.EndDate,
            p.IsActive)).ToList();
    }
}

// ============================================
// GET UPCOMING PROMOTIONS
// ============================================
public record GetUpcomingPromotionsQuery() : IRequest<List<PromotionSummaryDto>>;

public class GetUpcomingPromotionsQueryHandler : IRequestHandler<GetUpcomingPromotionsQuery, List<PromotionSummaryDto>>
{
    private readonly IPromotionRepository _promotionRepository;

    public GetUpcomingPromotionsQueryHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<List<PromotionSummaryDto>> Handle(GetUpcomingPromotionsQuery request, CancellationToken cancellationToken)
    {
        var promotions = await _promotionRepository.GetUpcomingPromotionsAsync(cancellationToken);

        return promotions.Select(p => new PromotionSummaryDto(
            p.Id,
            p.Code,
            p.Name,
            p.Type.ToString(),
            p.DiscountType.ToString(),
            p.DiscountValue,
            p.StartDate,
            p.EndDate,
            p.IsActive)).ToList();
    }
}

// ============================================
// GET PRODUCT PROMOTIONS
// ============================================
public record GetProductPromotionsQuery(Guid ProductId) : IRequest<List<PromotionSummaryDto>>;

public class GetProductPromotionsQueryHandler : IRequestHandler<GetProductPromotionsQuery, List<PromotionSummaryDto>>
{
    private readonly IPromotionRepository _promotionRepository;

    public GetProductPromotionsQueryHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<List<PromotionSummaryDto>> Handle(GetProductPromotionsQuery request, CancellationToken cancellationToken)
    {
        var promotions = await _promotionRepository.GetPromotionsForProductAsync(request.ProductId, cancellationToken);

        return promotions.Select(p => new PromotionSummaryDto(
            p.Id,
            p.Code,
            p.Name,
            p.Type.ToString(),
            p.DiscountType.ToString(),
            p.DiscountValue,
            p.StartDate,
            p.EndDate,
            p.IsActive)).ToList();
    }
}
