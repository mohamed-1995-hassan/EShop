using EShop.Domain.Aggregates.DigitalProductAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.DigitalProducts;

// Create Downloadable Product
public record CreateDownloadableProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    int? MaxDownloads = 5,
    int? AccessDurationDays = null) : IRequest<Guid>;

public class CreateDownloadableProductCommandHandler : IRequestHandler<CreateDownloadableProductCommand, Guid>
{
    private readonly IDigitalProductRepository _repository;

    public CreateDownloadableProductCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateDownloadableProductCommand request, CancellationToken cancellationToken)
    {
        var product = DigitalProduct.CreateDownloadable(
            request.ProductId,
            request.Name,
            request.Description,
            request.MaxDownloads,
            request.AccessDurationDays);

        await _repository.AddAsync(product, cancellationToken);
        return product.Id;
    }
}

// Create Software Product
public record CreateSoftwareProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    LicenseType LicenseType,
    int? MaxDevices = 1) : IRequest<Guid>;

public class CreateSoftwareProductCommandHandler : IRequestHandler<CreateSoftwareProductCommand, Guid>
{
    private readonly IDigitalProductRepository _repository;

    public CreateSoftwareProductCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateSoftwareProductCommand request, CancellationToken cancellationToken)
    {
        var product = DigitalProduct.CreateSoftware(
            request.ProductId,
            request.Name,
            request.Description,
            request.LicenseType,
            request.MaxDevices);

        await _repository.AddAsync(product, cancellationToken);
        return product.Id;
    }
}

// Create Ebook
public record CreateEbookCommand(
    Guid ProductId,
    string Name,
    string Description,
    bool AllowWatermarking = true) : IRequest<Guid>;

public class CreateEbookCommandHandler : IRequestHandler<CreateEbookCommand, Guid>
{
    private readonly IDigitalProductRepository _repository;

    public CreateEbookCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateEbookCommand request, CancellationToken cancellationToken)
    {
        var product = DigitalProduct.CreateEbook(
            request.ProductId,
            request.Name,
            request.Description,
            request.AllowWatermarking);

        await _repository.AddAsync(product, cancellationToken);
        return product.Id;
    }
}

// Add File
public record AddDigitalFileCommand(
    Guid DigitalProductId,
    string FileName,
    string FileUrl,
    long FileSizeBytes,
    string ContentType,
    string Version = "1.0",
    string? Checksum = null,
    FileType FileType = FileType.Primary) : IRequest<Guid>;

public class AddDigitalFileCommandHandler : IRequestHandler<AddDigitalFileCommand, Guid>
{
    private readonly IDigitalProductRepository _repository;

    public AddDigitalFileCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddDigitalFileCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        var file = product.AddFile(
            request.FileName,
            request.FileUrl,
            request.FileSizeBytes,
            request.ContentType,
            request.Version,
            request.Checksum,
            request.FileType);

        await _repository.UpdateAsync(product, cancellationToken);
        return file.Id;
    }
}

// Add License Key
public record AddLicenseKeyCommand(
    Guid DigitalProductId,
    string Key,
    DateTime? ExpiresAt = null) : IRequest<Guid>;

public class AddLicenseKeyCommandHandler : IRequestHandler<AddLicenseKeyCommand, Guid>
{
    private readonly IDigitalProductRepository _repository;

    public AddLicenseKeyCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<Guid> Handle(AddLicenseKeyCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        var license = product.AddLicenseKey(request.Key, request.ExpiresAt);
        await _repository.UpdateAsync(product, cancellationToken);
        return license.Id;
    }
}

// Import License Keys
public record ImportLicenseKeysCommand(
    Guid DigitalProductId,
    IEnumerable<string> Keys,
    DateTime? ExpiresAt = null) : IRequest;

public class ImportLicenseKeysCommandHandler : IRequestHandler<ImportLicenseKeysCommand>
{
    private readonly IDigitalProductRepository _repository;

    public ImportLicenseKeysCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task Handle(ImportLicenseKeysCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        product.ImportLicenseKeys(request.Keys, request.ExpiresAt);
        await _repository.UpdateAsync(product, cancellationToken);
    }
}

// Assign License Key
public record AssignLicenseKeyCommand(
    Guid DigitalProductId,
    Guid CustomerId,
    Guid OrderId) : IRequest<LicenseKey?>;

public class AssignLicenseKeyCommandHandler : IRequestHandler<AssignLicenseKeyCommand, LicenseKey?>
{
    private readonly IDigitalProductRepository _repository;

    public AssignLicenseKeyCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<LicenseKey?> Handle(AssignLicenseKeyCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        var license = product.AssignLicenseKey(request.CustomerId, request.OrderId);
        await _repository.UpdateAsync(product, cancellationToken);
        return license;
    }
}

// Revoke License
public record RevokeLicenseCommand(
    Guid DigitalProductId,
    Guid LicenseId,
    string Reason) : IRequest;

public class RevokeLicenseCommandHandler : IRequestHandler<RevokeLicenseCommand>
{
    private readonly IDigitalProductRepository _repository;

    public RevokeLicenseCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task Handle(RevokeLicenseCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        product.RevokeLicense(request.LicenseId, request.Reason);
        await _repository.UpdateAsync(product, cancellationToken);
    }
}

// Validate License
public record ValidateLicenseCommand(
    Guid DigitalProductId,
    string Key,
    string? DeviceId = null) : IRequest<LicenseValidationResult>;

public class ValidateLicenseCommandHandler : IRequestHandler<ValidateLicenseCommand, LicenseValidationResult>
{
    private readonly IDigitalProductRepository _repository;

    public ValidateLicenseCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<LicenseValidationResult> Handle(ValidateLicenseCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        var result = product.ValidateLicense(request.Key, request.DeviceId);
        
        if (request.DeviceId != null && result.IsValid)
            await _repository.UpdateAsync(product, cancellationToken);

        return result;
    }
}

// Activate/Deactivate
public record ActivateDigitalProductCommand(Guid DigitalProductId) : IRequest;

public class ActivateDigitalProductCommandHandler : IRequestHandler<ActivateDigitalProductCommand>
{
    private readonly IDigitalProductRepository _repository;

    public ActivateDigitalProductCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task Handle(ActivateDigitalProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        product.Activate();
        await _repository.UpdateAsync(product, cancellationToken);
    }
}

public record DeactivateDigitalProductCommand(Guid DigitalProductId) : IRequest;

public class DeactivateDigitalProductCommandHandler : IRequestHandler<DeactivateDigitalProductCommand>
{
    private readonly IDigitalProductRepository _repository;

    public DeactivateDigitalProductCommandHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task Handle(DeactivateDigitalProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DigitalProductId, cancellationToken)
            ?? throw new InvalidOperationException("Digital product not found");

        product.Deactivate();
        await _repository.UpdateAsync(product, cancellationToken);
    }
}
