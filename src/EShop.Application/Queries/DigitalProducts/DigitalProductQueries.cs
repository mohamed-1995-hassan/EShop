using EShop.Domain.Aggregates.DigitalProductAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.DigitalProducts;

// DTOs
public record DigitalProductDto(
    Guid Id,
    Guid ProductId,
    string Name,
    string Description,
    DigitalProductType Type,
    DeliveryMethod DeliveryMethod,
    bool RequiresLicense,
    LicenseType? LicenseType,
    int? MaxDownloads,
    int? MaxDevices,
    int? AccessDurationDays,
    bool AllowWatermarking,
    bool IsActive,
    int AvailableLicenseCount,
    bool HasAvailableLicenses,
    IReadOnlyList<DigitalFileDto> Files,
    IReadOnlyList<string> RestrictedCountries);

public record DigitalFileDto(
    Guid Id,
    string FileName,
    string FileUrl,
    long FileSizeBytes,
    string FileSizeFormatted,
    string ContentType,
    string Version,
    FileType FileType,
    bool IsPrimary,
    bool IsActive,
    DateTime UploadedAt);

public record LicenseKeyDto(
    Guid Id,
    string Key,
    LicenseType Type,
    int MaxDevices,
    bool IsAssigned,
    Guid? AssignedToCustomerId,
    DateTime? AssignedAt,
    DateTime? ExpiresAt,
    bool IsRevoked,
    bool IsValid,
    int AuthorizedDeviceCount);

public record DigitalProductSummaryDto(
    Guid Id,
    Guid ProductId,
    string Name,
    DigitalProductType Type,
    bool RequiresLicense,
    bool IsActive,
    int AvailableLicenseCount);

// Get Digital Product by ID
public record GetDigitalProductQuery(Guid Id) : IRequest<DigitalProductDto?>;

public class GetDigitalProductQueryHandler : IRequestHandler<GetDigitalProductQuery, DigitalProductDto?>
{
    private readonly IDigitalProductRepository _repository;

    public GetDigitalProductQueryHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<DigitalProductDto?> Handle(GetDigitalProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return product == null ? null : MapToDto(product);
    }

    private static DigitalProductDto MapToDto(DigitalProduct p) => new(
        p.Id,
        p.ProductId,
        p.Name,
        p.Description,
        p.Type,
        p.DeliveryMethod,
        p.RequiresLicense,
        p.LicenseType,
        p.MaxDownloads,
        p.MaxDevices,
        p.AccessDurationDays,
        p.AllowWatermarking,
        p.IsActive,
        p.AvailableLicenseCount,
        p.HasAvailableLicenses,
        p.Files.Select(f => new DigitalFileDto(
            f.Id, f.FileName, f.FileUrl, f.FileSizeBytes, f.FileSizeFormatted,
            f.ContentType, f.Version, f.FileType, f.IsPrimary, f.IsActive, f.UploadedAt))
            .ToList(),
        p.RestrictedCountries.ToList());
}

// Get Digital Product by Product ID
public record GetDigitalProductByProductIdQuery(Guid ProductId) : IRequest<DigitalProductDto?>;

public class GetDigitalProductByProductIdQueryHandler : IRequestHandler<GetDigitalProductByProductIdQuery, DigitalProductDto?>
{
    private readonly IDigitalProductRepository _repository;

    public GetDigitalProductByProductIdQueryHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<DigitalProductDto?> Handle(GetDigitalProductByProductIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (product == null) return null;

        return new DigitalProductDto(
            product.Id, product.ProductId, product.Name, product.Description,
            product.Type, product.DeliveryMethod, product.RequiresLicense, product.LicenseType,
            product.MaxDownloads, product.MaxDevices, product.AccessDurationDays,
            product.AllowWatermarking, product.IsActive, product.AvailableLicenseCount,
            product.HasAvailableLicenses,
            product.Files.Select(f => new DigitalFileDto(
                f.Id, f.FileName, f.FileUrl, f.FileSizeBytes, f.FileSizeFormatted,
                f.ContentType, f.Version, f.FileType, f.IsPrimary, f.IsActive, f.UploadedAt))
                .ToList(),
            product.RestrictedCountries.ToList());
    }
}

// Get Digital Products by Type
public record GetDigitalProductsByTypeQuery(DigitalProductType Type) : IRequest<IReadOnlyList<DigitalProductSummaryDto>>;

public class GetDigitalProductsByTypeQueryHandler : IRequestHandler<GetDigitalProductsByTypeQuery, IReadOnlyList<DigitalProductSummaryDto>>
{
    private readonly IDigitalProductRepository _repository;

    public GetDigitalProductsByTypeQueryHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<DigitalProductSummaryDto>> Handle(GetDigitalProductsByTypeQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.GetByTypeAsync(request.Type, cancellationToken);
        return products.Select(p => new DigitalProductSummaryDto(
            p.Id, p.ProductId, p.Name, p.Type, p.RequiresLicense, p.IsActive, p.AvailableLicenseCount)).ToList();
    }
}

// Get Active Digital Products
public record GetActiveDigitalProductsQuery : IRequest<IReadOnlyList<DigitalProductSummaryDto>>;

public class GetActiveDigitalProductsQueryHandler : IRequestHandler<GetActiveDigitalProductsQuery, IReadOnlyList<DigitalProductSummaryDto>>
{
    private readonly IDigitalProductRepository _repository;

    public GetActiveDigitalProductsQueryHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<DigitalProductSummaryDto>> Handle(GetActiveDigitalProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.GetActiveAsync(cancellationToken);
        return products.Select(p => new DigitalProductSummaryDto(
            p.Id, p.ProductId, p.Name, p.Type, p.RequiresLicense, p.IsActive, p.AvailableLicenseCount)).ToList();
    }
}

// Get Customer Licenses
public record GetCustomerLicensesQuery(Guid CustomerId) : IRequest<IReadOnlyList<LicenseKeyDto>>;

public class GetCustomerLicensesQueryHandler : IRequestHandler<GetCustomerLicensesQuery, IReadOnlyList<LicenseKeyDto>>
{
    private readonly IDigitalProductRepository _repository;

    public GetCustomerLicensesQueryHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<LicenseKeyDto>> Handle(GetCustomerLicensesQuery request, CancellationToken cancellationToken)
    {
        var licenses = await _repository.GetLicensesByCustomerIdAsync(request.CustomerId, cancellationToken);
        return licenses.Select(l => new LicenseKeyDto(
            l.Id, l.Key, l.Type, l.MaxDevices, l.IsAssigned, l.AssignedToCustomerId,
            l.AssignedAt, l.ExpiresAt, l.IsRevoked, l.IsValid, l.Devices.Count(d => d.IsAuthorized))).ToList();
    }
}

// Lookup License by Key
public record LookupLicenseQuery(string Key) : IRequest<LicenseKeyDto?>;

public class LookupLicenseQueryHandler : IRequestHandler<LookupLicenseQuery, LicenseKeyDto?>
{
    private readonly IDigitalProductRepository _repository;

    public LookupLicenseQueryHandler(IDigitalProductRepository repository) => _repository = repository;

    public async Task<LicenseKeyDto?> Handle(LookupLicenseQuery request, CancellationToken cancellationToken)
    {
        var license = await _repository.GetLicenseByKeyAsync(request.Key, cancellationToken);
        if (license == null) return null;

        return new LicenseKeyDto(
            license.Id, license.Key, license.Type, license.MaxDevices,
            license.IsAssigned, license.AssignedToCustomerId, license.AssignedAt,
            license.ExpiresAt, license.IsRevoked, license.IsValid,
            license.Devices.Count(d => d.IsAuthorized));
    }
}
