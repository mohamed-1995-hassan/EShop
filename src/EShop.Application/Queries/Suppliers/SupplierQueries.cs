using EShop.Domain.Aggregates.SupplierAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Suppliers;

public record SupplierDto(Guid Id, string Code, string Name, string ContactName, string Email, SupplierStatus Status, SupplierTier Tier, string Currency, int DefaultLeadTimeDays, decimal PerformanceScore, decimal OnTimeDeliveryRate, int TotalOrders);
public record SupplierSummaryDto(Guid Id, string Code, string Name, SupplierStatus Status, SupplierTier Tier, decimal PerformanceScore);
public record PurchaseOrderDto(Guid Id, string PoNumber, Guid SupplierId, string SupplierName, PurchaseOrderStatus Status, decimal Total, string Currency, DateTime? ExpectedDeliveryDate, int TotalQuantity, int ReceivedQuantity);

public record GetSupplierQuery(Guid Id) : IRequest<SupplierDto?>;
public record GetActiveSupliersQuery : IRequest<IReadOnlyList<SupplierSummaryDto>>;
public record GetSuppliersForProductQuery(Guid ProductId) : IRequest<IReadOnlyList<SupplierSummaryDto>>;
public record GetPurchaseOrderQuery(Guid Id) : IRequest<PurchaseOrderDto?>;
public record GetPendingApprovalPOsQuery : IRequest<IReadOnlyList<PurchaseOrderDto>>;
public record GetAwaitingDeliveryPOsQuery : IRequest<IReadOnlyList<PurchaseOrderDto>>;

public class GetSupplierHandler : IRequestHandler<GetSupplierQuery, SupplierDto?>
{
    private readonly ISupplierRepository _repo;
    public GetSupplierHandler(ISupplierRepository repo) => _repo = repo;

    public async Task<SupplierDto?> Handle(GetSupplierQuery request, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(request.Id, ct);
        return s == null ? null : new SupplierDto(s.Id, s.Code, s.Name, s.ContactName, s.Email, s.Status, s.Tier, s.Currency, s.DefaultLeadTimeDays, s.PerformanceScore, s.OnTimeDeliveryRate, s.TotalOrders);
    }
}

public class GetActiveSuppliersHandler : IRequestHandler<GetActiveSupliersQuery, IReadOnlyList<SupplierSummaryDto>>
{
    private readonly ISupplierRepository _repo;
    public GetActiveSuppliersHandler(ISupplierRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SupplierSummaryDto>> Handle(GetActiveSupliersQuery request, CancellationToken ct)
    {
        var suppliers = await _repo.GetActiveAsync(ct);
        return suppliers.Select(s => new SupplierSummaryDto(s.Id, s.Code, s.Name, s.Status, s.Tier, s.PerformanceScore)).ToList();
    }
}

public class GetSuppliersForProductHandler : IRequestHandler<GetSuppliersForProductQuery, IReadOnlyList<SupplierSummaryDto>>
{
    private readonly ISupplierRepository _repo;
    public GetSuppliersForProductHandler(ISupplierRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SupplierSummaryDto>> Handle(GetSuppliersForProductQuery request, CancellationToken ct)
    {
        var suppliers = await _repo.GetSuppliersForProductAsync(request.ProductId, ct);
        return suppliers.Select(s => new SupplierSummaryDto(s.Id, s.Code, s.Name, s.Status, s.Tier, s.PerformanceScore)).ToList();
    }
}

public class GetPurchaseOrderHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderDto?>
{
    private readonly IPurchaseOrderRepository _repo;
    public GetPurchaseOrderHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<PurchaseOrderDto?> Handle(GetPurchaseOrderQuery request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.Id, ct);
        return po == null ? null : new PurchaseOrderDto(po.Id, po.PoNumber, po.SupplierId, po.SupplierName, po.Status, po.Total.Amount, po.Total.Currency, po.ExpectedDeliveryDate, po.TotalQuantity, po.ReceivedQuantity);
    }
}

public class GetPendingApprovalPOsHandler : IRequestHandler<GetPendingApprovalPOsQuery, IReadOnlyList<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderRepository _repo;
    public GetPendingApprovalPOsHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<PurchaseOrderDto>> Handle(GetPendingApprovalPOsQuery request, CancellationToken ct)
    {
        var orders = await _repo.GetPendingApprovalAsync(ct);
        return orders.Select(po => new PurchaseOrderDto(po.Id, po.PoNumber, po.SupplierId, po.SupplierName, po.Status, po.Total.Amount, po.Total.Currency, po.ExpectedDeliveryDate, po.TotalQuantity, po.ReceivedQuantity)).ToList();
    }
}

public class GetAwaitingDeliveryPOsHandler : IRequestHandler<GetAwaitingDeliveryPOsQuery, IReadOnlyList<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderRepository _repo;
    public GetAwaitingDeliveryPOsHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<PurchaseOrderDto>> Handle(GetAwaitingDeliveryPOsQuery request, CancellationToken ct)
    {
        var orders = await _repo.GetAwaitingDeliveryAsync(ct);
        return orders.Select(po => new PurchaseOrderDto(po.Id, po.PoNumber, po.SupplierId, po.SupplierName, po.Status, po.Total.Amount, po.Total.Currency, po.ExpectedDeliveryDate, po.TotalQuantity, po.ReceivedQuantity)).ToList();
    }
}
