using EShop.Domain.Aggregates.BundleAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Bundles;

// DTOs
public record BundleDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    string? ImageUrl,
    BundleType Type,
    BundleStatus Status,
    decimal BasePrice,
    decimal BundlePrice,
    decimal SavingsAmount,
    decimal SavingsPercentage,
    string Currency,
    DiscountType DiscountType,
    decimal DiscountValue,
    int? MaxQuantityPerOrder,
    int MinItemsRequired,
    int MaxItemsAllowed,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    bool IsActive,
    IReadOnlyList<BundleItemDto> Items);

public record BundleItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    bool IsRequired,
    int MinQuantity,
    int MaxQuantity,
    decimal TotalPrice);

public record BundleSummaryDto(
    Guid Id,
    string Sku,
    string Name,
    BundleType Type,
    BundleStatus Status,
    decimal BundlePrice,
    decimal SavingsPercentage,
    string Currency,
    int ItemCount,
    bool IsActive);

// Get Bundle by ID
public record GetBundleQuery(Guid Id) : IRequest<BundleDto?>;

public class GetBundleQueryHandler : IRequestHandler<GetBundleQuery, BundleDto?>
{
    private readonly IBundleRepository _repository;

    public GetBundleQueryHandler(IBundleRepository repository) => _repository = repository;

    public async Task<BundleDto?> Handle(GetBundleQuery request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return bundle == null ? null : MapToDto(bundle);
    }

    private static BundleDto MapToDto(Bundle b) => new(
        b.Id,
        b.Sku,
        b.Name,
        b.Description,
        b.ImageUrl,
        b.Type,
        b.Status,
        b.BasePrice.Amount,
        b.BundlePrice.Amount,
        b.SavingsAmount.Amount,
        b.SavingsPercentage,
        b.BundlePrice.Currency,
        b.DiscountType,
        b.DiscountValue,
        b.MaxQuantityPerOrder,
        b.MinItemsRequired,
        b.MaxItemsAllowed,
        b.ValidFrom,
        b.ValidUntil,
        b.IsActive,
        b.Items.Select(i => new BundleItemDto(
            i.Id, i.ProductId, i.ProductName, i.ProductSku,
            i.UnitPrice.Amount, i.UnitPrice.Currency, i.Quantity,
            i.IsRequired, i.MinQuantity, i.MaxQuantity, i.TotalPrice.Amount))
            .ToList());
}

// Get Bundle by SKU
public record GetBundleBySkuQuery(string Sku) : IRequest<BundleDto?>;

public class GetBundleBySkuQueryHandler : IRequestHandler<GetBundleBySkuQuery, BundleDto?>
{
    private readonly IBundleRepository _repository;

    public GetBundleBySkuQueryHandler(IBundleRepository repository) => _repository = repository;

    public async Task<BundleDto?> Handle(GetBundleBySkuQuery request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetBySkuAsync(request.Sku, cancellationToken);
        if (bundle == null) return null;

        return new BundleDto(
            bundle.Id, bundle.Sku, bundle.Name, bundle.Description, bundle.ImageUrl,
            bundle.Type, bundle.Status, bundle.BasePrice.Amount, bundle.BundlePrice.Amount,
            bundle.SavingsAmount.Amount, bundle.SavingsPercentage, bundle.BundlePrice.Currency,
            bundle.DiscountType, bundle.DiscountValue, bundle.MaxQuantityPerOrder,
            bundle.MinItemsRequired, bundle.MaxItemsAllowed, bundle.ValidFrom, bundle.ValidUntil,
            bundle.IsActive,
            bundle.Items.Select(i => new BundleItemDto(
                i.Id, i.ProductId, i.ProductName, i.ProductSku,
                i.UnitPrice.Amount, i.UnitPrice.Currency, i.Quantity,
                i.IsRequired, i.MinQuantity, i.MaxQuantity, i.TotalPrice.Amount))
                .ToList());
    }
}

// Get Active Bundles
public record GetActiveBundlesQuery : IRequest<IReadOnlyList<BundleSummaryDto>>;

public class GetActiveBundlesQueryHandler : IRequestHandler<GetActiveBundlesQuery, IReadOnlyList<BundleSummaryDto>>
{
    private readonly IBundleRepository _repository;

    public GetActiveBundlesQueryHandler(IBundleRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<BundleSummaryDto>> Handle(GetActiveBundlesQuery request, CancellationToken cancellationToken)
    {
        var bundles = await _repository.GetActiveAsync(cancellationToken);
        return bundles.Select(b => new BundleSummaryDto(
            b.Id, b.Sku, b.Name, b.Type, b.Status, b.BundlePrice.Amount,
            b.SavingsPercentage, b.BundlePrice.Currency, b.Items.Count, b.IsActive)).ToList();
    }
}

// Get Bundles by Type
public record GetBundlesByTypeQuery(BundleType Type) : IRequest<IReadOnlyList<BundleSummaryDto>>;

public class GetBundlesByTypeQueryHandler : IRequestHandler<GetBundlesByTypeQuery, IReadOnlyList<BundleSummaryDto>>
{
    private readonly IBundleRepository _repository;

    public GetBundlesByTypeQueryHandler(IBundleRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<BundleSummaryDto>> Handle(GetBundlesByTypeQuery request, CancellationToken cancellationToken)
    {
        var bundles = await _repository.GetByTypeAsync(request.Type, cancellationToken);
        return bundles.Select(b => new BundleSummaryDto(
            b.Id, b.Sku, b.Name, b.Type, b.Status, b.BundlePrice.Amount,
            b.SavingsPercentage, b.BundlePrice.Currency, b.Items.Count, b.IsActive)).ToList();
    }
}

// Get Bundles Containing Product
public record GetBundlesContainingProductQuery(Guid ProductId) : IRequest<IReadOnlyList<BundleSummaryDto>>;

public class GetBundlesContainingProductQueryHandler : IRequestHandler<GetBundlesContainingProductQuery, IReadOnlyList<BundleSummaryDto>>
{
    private readonly IBundleRepository _repository;

    public GetBundlesContainingProductQueryHandler(IBundleRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<BundleSummaryDto>> Handle(GetBundlesContainingProductQuery request, CancellationToken cancellationToken)
    {
        var bundles = await _repository.GetBundlesContainingProductAsync(request.ProductId, cancellationToken);
        return bundles.Select(b => new BundleSummaryDto(
            b.Id, b.Sku, b.Name, b.Type, b.Status, b.BundlePrice.Amount,
            b.SavingsPercentage, b.BundlePrice.Currency, b.Items.Count, b.IsActive)).ToList();
    }
}

// Validate Bundle Selection
public record ValidateBundleSelectionQuery(
    Guid BundleId,
    Dictionary<Guid, int> SelectedQuantities) : IRequest<BundleValidationResult>;

public class ValidateBundleSelectionQueryHandler : IRequestHandler<ValidateBundleSelectionQuery, BundleValidationResult>
{
    private readonly IBundleRepository _repository;

    public ValidateBundleSelectionQueryHandler(IBundleRepository repository) => _repository = repository;

    public async Task<BundleValidationResult> Handle(ValidateBundleSelectionQuery request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        return bundle.ValidateSelection(request.SelectedQuantities);
    }
}

// Calculate Bundle Price
public record CalculateBundlePriceQuery(
    Guid BundleId,
    Dictionary<Guid, int>? SelectedQuantities = null) : IRequest<decimal>;

public class CalculateBundlePriceQueryHandler : IRequestHandler<CalculateBundlePriceQuery, decimal>
{
    private readonly IBundleRepository _repository;

    public CalculateBundlePriceQueryHandler(IBundleRepository repository) => _repository = repository;

    public async Task<decimal> Handle(CalculateBundlePriceQuery request, CancellationToken cancellationToken)
    {
        var bundle = await _repository.GetByIdAsync(request.BundleId, cancellationToken)
            ?? throw new InvalidOperationException("Bundle not found");

        return bundle.CalculatePrice(request.SelectedQuantities).Amount;
    }
}
