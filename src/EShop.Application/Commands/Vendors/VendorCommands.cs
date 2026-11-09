using EShop.Domain.Aggregates.VendorAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Vendors;

// Register Vendor
public record RegisterVendorCommand(
    string BusinessName,
    string LegalName,
    string ContactEmail,
    string ContactPhone,
    string Street,
    string City,
    string Country,
    string PostalCode,
    string? TaxId = null) : IRequest<Guid>;

public class RegisterVendorCommandHandler : IRequestHandler<RegisterVendorCommand, Guid>
{
    private readonly IVendorRepository _repository;

    public RegisterVendorCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task<Guid> Handle(RegisterVendorCommand request, CancellationToken cancellationToken)
    {
        var address = new Address(request.Street, request.City, request.Country, request.PostalCode);
        var vendor = Vendor.Register(
            request.BusinessName,
            request.LegalName,
            request.ContactEmail,
            request.ContactPhone,
            address,
            request.TaxId);

        await _repository.AddAsync(vendor, cancellationToken);
        return vendor.Id;
    }
}

// Submit for Approval
public record SubmitVendorForApprovalCommand(Guid VendorId) : IRequest;

public class SubmitVendorForApprovalCommandHandler : IRequestHandler<SubmitVendorForApprovalCommand>
{
    private readonly IVendorRepository _repository;

    public SubmitVendorForApprovalCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task Handle(SubmitVendorForApprovalCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Vendor not found");

        vendor.SubmitForApproval();
        await _repository.UpdateAsync(vendor, cancellationToken);
    }
}

// Approve Vendor
public record ApproveVendorCommand(Guid VendorId, Guid ApprovedBy, string? Notes = null) : IRequest;

public class ApproveVendorCommandHandler : IRequestHandler<ApproveVendorCommand>
{
    private readonly IVendorRepository _repository;

    public ApproveVendorCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task Handle(ApproveVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Vendor not found");

        vendor.Approve(request.ApprovedBy, request.Notes);
        await _repository.UpdateAsync(vendor, cancellationToken);
    }
}

// Reject Vendor
public record RejectVendorCommand(Guid VendorId, Guid RejectedBy, string Reason) : IRequest;

public class RejectVendorCommandHandler : IRequestHandler<RejectVendorCommand>
{
    private readonly IVendorRepository _repository;

    public RejectVendorCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task Handle(RejectVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Vendor not found");

        vendor.Reject(request.RejectedBy, request.Reason);
        await _repository.UpdateAsync(vendor, cancellationToken);
    }
}

// Suspend Vendor
public record SuspendVendorCommand(Guid VendorId, Guid SuspendedBy, string Reason) : IRequest;

public class SuspendVendorCommandHandler : IRequestHandler<SuspendVendorCommand>
{
    private readonly IVendorRepository _repository;

    public SuspendVendorCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task Handle(SuspendVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Vendor not found");

        vendor.Suspend(request.SuspendedBy, request.Reason);
        await _repository.UpdateAsync(vendor, cancellationToken);
    }
}

// Add Category
public record AddVendorCategoryCommand(
    Guid VendorId,
    Guid CategoryId,
    string CategoryName,
    decimal? CustomCommissionRate = null) : IRequest<Guid>;

public class AddVendorCategoryCommandHandler : IRequestHandler<AddVendorCategoryCommand, Guid>
{
    private readonly IVendorRepository _repository;

    public AddVendorCategoryCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddVendorCategoryCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Vendor not found");

        var category = vendor.AddCategory(request.CategoryId, request.CategoryName, request.CustomCommissionRate);
        await _repository.UpdateAsync(vendor, cancellationToken);
        return category.Id;
    }
}

// Add Document
public record AddVendorDocumentCommand(
    Guid VendorId,
    DocumentType Type,
    string DocumentUrl,
    string? Description = null) : IRequest<Guid>;

public class AddVendorDocumentCommandHandler : IRequestHandler<AddVendorDocumentCommand, Guid>
{
    private readonly IVendorRepository _repository;

    public AddVendorDocumentCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddVendorDocumentCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Vendor not found");

        var document = vendor.AddDocument(request.Type, request.DocumentUrl, request.Description);
        await _repository.UpdateAsync(vendor, cancellationToken);
        return document.Id;
    }
}

// Record Sale
public record RecordVendorSaleCommand(Guid VendorId, int ProductsSold, decimal Revenue, string Currency) : IRequest;

public class RecordVendorSaleCommandHandler : IRequestHandler<RecordVendorSaleCommand>
{
    private readonly IVendorRepository _repository;

    public RecordVendorSaleCommandHandler(IVendorRepository repository) => _repository = repository;

    public async Task Handle(RecordVendorSaleCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Vendor not found");

        vendor.RecordSale(request.ProductsSold, new Money(request.Revenue, request.Currency));
        await _repository.UpdateAsync(vendor, cancellationToken);
    }
}
