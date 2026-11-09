using EShop.Domain.Aggregates.SupplierAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Suppliers;

public record CreateSupplierCommand(string Code, string Name, string ContactName, string Email, string Currency = "USD") : IRequest<Guid>;
public record ApproveSupplierCommand(Guid SupplierId) : IRequest;
public record SuspendSupplierCommand(Guid SupplierId, string Reason) : IRequest;
public record AddSupplierProductCommand(Guid SupplierId, Guid ProductId, string Sku, string Name, decimal UnitCost, string Currency, int MinOrderQty = 1, int? LeadTimeDays = null) : IRequest<Guid>;
public record UpdateSupplierProductCostCommand(Guid SupplierId, Guid ProductId, decimal NewCost, string Currency) : IRequest;
public record RecordSupplierDeliveryCommand(Guid SupplierId, bool OnTime, bool HasQualityIssue) : IRequest;

public record CreatePurchaseOrderCommand(Guid SupplierId, string SupplierName, Guid CreatedBy, string Currency, Guid? WarehouseId = null) : IRequest<Guid>;
public record AddPurchaseOrderLineCommand(Guid PurchaseOrderId, Guid ProductId, string ProductName, string Sku, int Quantity, decimal UnitCost, string Currency) : IRequest<Guid>;
public record SubmitPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest;
public record ApprovePurchaseOrderCommand(Guid PurchaseOrderId, Guid ApprovedBy) : IRequest;
public record SendPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest;
public record ReceivePurchaseOrderCommand(Guid PurchaseOrderId, Guid ReceivedBy, Dictionary<Guid, int> QuantitiesReceived, string? Notes = null) : IRequest<Guid>;

public class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly ISupplierRepository _repo;
    public CreateSupplierHandler(ISupplierRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var supplier = Supplier.Create(request.Code, request.Name, request.ContactName, request.Email, request.Currency);
        await _repo.AddAsync(supplier, ct);
        return supplier.Id;
    }
}

public class ApproveSupplierHandler : IRequestHandler<ApproveSupplierCommand>
{
    private readonly ISupplierRepository _repo;
    public ApproveSupplierHandler(ISupplierRepository repo) => _repo = repo;

    public async Task Handle(ApproveSupplierCommand request, CancellationToken ct)
    {
        var supplier = await _repo.GetByIdAsync(request.SupplierId, ct) ?? throw new InvalidOperationException("Supplier not found");
        supplier.Approve();
        await _repo.UpdateAsync(supplier, ct);
    }
}

public class SuspendSupplierHandler : IRequestHandler<SuspendSupplierCommand>
{
    private readonly ISupplierRepository _repo;
    public SuspendSupplierHandler(ISupplierRepository repo) => _repo = repo;

    public async Task Handle(SuspendSupplierCommand request, CancellationToken ct)
    {
        var supplier = await _repo.GetByIdAsync(request.SupplierId, ct) ?? throw new InvalidOperationException("Supplier not found");
        supplier.Suspend(request.Reason);
        await _repo.UpdateAsync(supplier, ct);
    }
}

public class AddSupplierProductHandler : IRequestHandler<AddSupplierProductCommand, Guid>
{
    private readonly ISupplierRepository _repo;
    public AddSupplierProductHandler(ISupplierRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddSupplierProductCommand request, CancellationToken ct)
    {
        var supplier = await _repo.GetByIdAsync(request.SupplierId, ct) ?? throw new InvalidOperationException("Supplier not found");
        var product = supplier.AddProduct(request.ProductId, request.Sku, request.Name, new Money(request.UnitCost, request.Currency), request.MinOrderQty, request.LeadTimeDays);
        await _repo.UpdateAsync(supplier, ct);
        return product.Id;
    }
}

public class UpdateSupplierProductCostHandler : IRequestHandler<UpdateSupplierProductCostCommand>
{
    private readonly ISupplierRepository _repo;
    public UpdateSupplierProductCostHandler(ISupplierRepository repo) => _repo = repo;

    public async Task Handle(UpdateSupplierProductCostCommand request, CancellationToken ct)
    {
        var supplier = await _repo.GetByIdAsync(request.SupplierId, ct) ?? throw new InvalidOperationException("Supplier not found");
        supplier.UpdateProductCost(request.ProductId, new Money(request.NewCost, request.Currency));
        await _repo.UpdateAsync(supplier, ct);
    }
}

public class RecordSupplierDeliveryHandler : IRequestHandler<RecordSupplierDeliveryCommand>
{
    private readonly ISupplierRepository _repo;
    public RecordSupplierDeliveryHandler(ISupplierRepository repo) => _repo = repo;

    public async Task Handle(RecordSupplierDeliveryCommand request, CancellationToken ct)
    {
        var supplier = await _repo.GetByIdAsync(request.SupplierId, ct) ?? throw new InvalidOperationException("Supplier not found");
        supplier.RecordDelivery(request.OnTime, request.HasQualityIssue);
        await _repo.UpdateAsync(supplier, ct);
    }
}

public class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    private readonly IPurchaseOrderRepository _repo;
    public CreatePurchaseOrderHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = PurchaseOrder.Create(request.SupplierId, request.SupplierName, request.CreatedBy, request.Currency, request.WarehouseId);
        await _repo.AddAsync(po, ct);
        return po.Id;
    }
}

public class AddPurchaseOrderLineHandler : IRequestHandler<AddPurchaseOrderLineCommand, Guid>
{
    private readonly IPurchaseOrderRepository _repo;
    public AddPurchaseOrderLineHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddPurchaseOrderLineCommand request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.PurchaseOrderId, ct) ?? throw new InvalidOperationException("Purchase order not found");
        var line = po.AddLine(request.ProductId, request.ProductName, request.Sku, request.Quantity, new Money(request.UnitCost, request.Currency));
        await _repo.UpdateAsync(po, ct);
        return line.Id;
    }
}

public class SubmitPurchaseOrderHandler : IRequestHandler<SubmitPurchaseOrderCommand>
{
    private readonly IPurchaseOrderRepository _repo;
    public SubmitPurchaseOrderHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task Handle(SubmitPurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.PurchaseOrderId, ct) ?? throw new InvalidOperationException("Purchase order not found");
        po.Submit();
        await _repo.UpdateAsync(po, ct);
    }
}

public class ApprovePurchaseOrderHandler : IRequestHandler<ApprovePurchaseOrderCommand>
{
    private readonly IPurchaseOrderRepository _repo;
    public ApprovePurchaseOrderHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task Handle(ApprovePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.PurchaseOrderId, ct) ?? throw new InvalidOperationException("Purchase order not found");
        po.Approve(request.ApprovedBy);
        await _repo.UpdateAsync(po, ct);
    }
}

public class SendPurchaseOrderHandler : IRequestHandler<SendPurchaseOrderCommand>
{
    private readonly IPurchaseOrderRepository _repo;
    public SendPurchaseOrderHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task Handle(SendPurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.PurchaseOrderId, ct) ?? throw new InvalidOperationException("Purchase order not found");
        po.SendToSupplier();
        await _repo.UpdateAsync(po, ct);
    }
}

public class ReceivePurchaseOrderHandler : IRequestHandler<ReceivePurchaseOrderCommand, Guid>
{
    private readonly IPurchaseOrderRepository _repo;
    public ReceivePurchaseOrderHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<Guid> Handle(ReceivePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.PurchaseOrderId, ct) ?? throw new InvalidOperationException("Purchase order not found");
        var receipt = po.ReceiveGoods(request.ReceivedBy, request.QuantitiesReceived, request.Notes);
        await _repo.UpdateAsync(po, ct);
        return receipt.Id;
    }
}
