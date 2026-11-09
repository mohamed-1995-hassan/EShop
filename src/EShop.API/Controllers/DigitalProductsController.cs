using EShop.Application.Commands.DigitalProducts;
using EShop.Application.Queries.DigitalProducts;
using EShop.Domain.Aggregates.DigitalProductAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/digital-products")]
public class DigitalProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DigitalProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DigitalProductDto>> Get(Guid id)
    {
        var product = await _mediator.Send(new GetDigitalProductQuery(id));
        return product != null ? Ok(product) : NotFound();
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<DigitalProductDto>> GetByProductId(Guid productId)
    {
        var product = await _mediator.Send(new GetDigitalProductByProductIdQuery(productId));
        return product != null ? Ok(product) : NotFound();
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<IReadOnlyList<DigitalProductSummaryDto>>> GetByType(DigitalProductType type)
    {
        var products = await _mediator.Send(new GetDigitalProductsByTypeQuery(type));
        return Ok(products);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<DigitalProductSummaryDto>>> GetActive()
    {
        var products = await _mediator.Send(new GetActiveDigitalProductsQuery());
        return Ok(products);
    }

    [HttpGet("licenses/customer/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LicenseKeyDto>>> GetCustomerLicenses(Guid customerId)
    {
        var licenses = await _mediator.Send(new GetCustomerLicensesQuery(customerId));
        return Ok(licenses);
    }

    [HttpGet("licenses/lookup/{key}")]
    public async Task<ActionResult<LicenseKeyDto>> LookupLicense(string key)
    {
        var license = await _mediator.Send(new LookupLicenseQuery(key));
        return license != null ? Ok(license) : NotFound();
    }

    [HttpPost("downloadable")]
    public async Task<ActionResult<Guid>> CreateDownloadable([FromBody] CreateDownloadableRequest request)
    {
        var id = await _mediator.Send(new CreateDownloadableProductCommand(
            request.ProductId, request.Name, request.Description, request.MaxDownloads, request.AccessDurationDays));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("software")]
    public async Task<ActionResult<Guid>> CreateSoftware([FromBody] CreateSoftwareRequest request)
    {
        var id = await _mediator.Send(new CreateSoftwareProductCommand(
            request.ProductId, request.Name, request.Description, request.LicenseType, request.MaxDevices));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("ebook")]
    public async Task<ActionResult<Guid>> CreateEbook([FromBody] CreateEbookRequest request)
    {
        var id = await _mediator.Send(new CreateEbookCommand(
            request.ProductId, request.Name, request.Description, request.AllowWatermarking));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{digitalProductId:guid}/files")]
    public async Task<ActionResult<Guid>> AddFile(Guid digitalProductId, [FromBody] AddFileRequest request)
    {
        var fileId = await _mediator.Send(new AddDigitalFileCommand(
            digitalProductId, request.FileName, request.FileUrl, request.FileSizeBytes,
            request.ContentType, request.Version, request.Checksum, request.FileType));
        return Ok(fileId);
    }

    [HttpPost("{digitalProductId:guid}/licenses")]
    public async Task<ActionResult<Guid>> AddLicense(Guid digitalProductId, [FromBody] AddLicenseRequest request)
    {
        var licenseId = await _mediator.Send(new AddLicenseKeyCommand(
            digitalProductId, request.Key, request.ExpiresAt));
        return Ok(licenseId);
    }

    [HttpPost("{digitalProductId:guid}/licenses/import")]
    public async Task<IActionResult> ImportLicenses(Guid digitalProductId, [FromBody] ImportLicensesRequest request)
    {
        await _mediator.Send(new ImportLicenseKeysCommand(digitalProductId, request.Keys, request.ExpiresAt));
        return NoContent();
    }

    [HttpPost("{digitalProductId:guid}/licenses/assign")]
    public async Task<ActionResult<LicenseKey?>> AssignLicense(Guid digitalProductId, [FromBody] AssignLicenseRequest request)
    {
        var license = await _mediator.Send(new AssignLicenseKeyCommand(
            digitalProductId, request.CustomerId, request.OrderId));
        return Ok(license);
    }

    [HttpPost("{digitalProductId:guid}/licenses/{licenseId:guid}/revoke")]
    public async Task<IActionResult> RevokeLicense(Guid digitalProductId, Guid licenseId, [FromBody] RevokeLicenseRequest request)
    {
        await _mediator.Send(new RevokeLicenseCommand(digitalProductId, licenseId, request.Reason));
        return NoContent();
    }

    [HttpPost("{digitalProductId:guid}/licenses/validate")]
    public async Task<ActionResult<LicenseValidationResult>> ValidateLicense(Guid digitalProductId, [FromBody] ValidateLicenseRequest request)
    {
        var result = await _mediator.Send(new ValidateLicenseCommand(digitalProductId, request.Key, request.DeviceId));
        return Ok(result);
    }

    [HttpPost("{digitalProductId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid digitalProductId)
    {
        await _mediator.Send(new ActivateDigitalProductCommand(digitalProductId));
        return NoContent();
    }

    [HttpPost("{digitalProductId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid digitalProductId)
    {
        await _mediator.Send(new DeactivateDigitalProductCommand(digitalProductId));
        return NoContent();
    }
}

// Request DTOs
public record CreateDownloadableRequest(
    Guid ProductId,
    string Name,
    string Description,
    int? MaxDownloads = 5,
    int? AccessDurationDays = null);

public record CreateSoftwareRequest(
    Guid ProductId,
    string Name,
    string Description,
    LicenseType LicenseType,
    int? MaxDevices = 1);

public record CreateEbookRequest(
    Guid ProductId,
    string Name,
    string Description,
    bool AllowWatermarking = true);

public record AddFileRequest(
    string FileName,
    string FileUrl,
    long FileSizeBytes,
    string ContentType,
    string Version = "1.0",
    string? Checksum = null,
    FileType FileType = FileType.Primary);

public record AddLicenseRequest(string Key, DateTime? ExpiresAt = null);
public record ImportLicensesRequest(IEnumerable<string> Keys, DateTime? ExpiresAt = null);
public record AssignLicenseRequest(Guid CustomerId, Guid OrderId);
public record RevokeLicenseRequest(string Reason);
public record ValidateLicenseRequest(string Key, string? DeviceId = null);
