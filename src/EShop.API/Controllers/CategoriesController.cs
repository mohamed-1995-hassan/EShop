using EShop.Application.Commands.Catalog;
using EShop.Application.Queries.Catalog;
using EShop.Domain.Aggregates.CatalogAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Get(Guid id)
    {
        var category = await _mediator.Send(new GetCategoryQuery(id));
        return category != null ? Ok(category) : NotFound();
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<CategoryDto>> GetBySlug(string slug)
    {
        var category = await _mediator.Send(new GetCategoryBySlugQuery(slug));
        return category != null ? Ok(category) : NotFound();
    }

    [HttpGet("root")]
    public async Task<ActionResult<IReadOnlyList<CategorySummaryDto>>> GetRootCategories()
    {
        var categories = await _mediator.Send(new GetRootCategoriesQuery());
        return Ok(categories);
    }

    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<IReadOnlyList<CategorySummaryDto>>> GetChildren(Guid id)
    {
        var categories = await _mediator.Send(new GetChildCategoriesQuery(id));
        return Ok(categories);
    }

    [HttpGet("featured")]
    public async Task<ActionResult<IReadOnlyList<CategorySummaryDto>>> GetFeatured()
    {
        var categories = await _mediator.Send(new GetFeaturedCategoriesQuery());
        return Ok(categories);
    }

    [HttpPost("root")]
    public async Task<ActionResult<Guid>> CreateRoot([FromBody] CreateRootCategoryRequest request)
    {
        var id = await _mediator.Send(new CreateRootCategoryCommand(request.Name, request.Description, request.ImageUrl));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("child")]
    public async Task<ActionResult<Guid>> CreateChild([FromBody] CreateChildCategoryRequest request)
    {
        var id = await _mediator.Send(new CreateChildCategoryCommand(request.Name, request.ParentId, request.Level, request.Description));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        await _mediator.Send(new UpdateCategoryCommand(id, request.Name, request.Description, request.ImageUrl, request.IconUrl));
        return NoContent();
    }

    [HttpPost("{id:guid}/attributes")]
    public async Task<ActionResult<Guid>> AddAttribute(Guid id, [FromBody] AddAttributeRequest request)
    {
        var attrId = await _mediator.Send(new AddCategoryAttributeCommand(id, request.Name, request.Type, request.IsFilterable, request.IsSearchable));
        return Ok(attrId);
    }

    [HttpPost("{id:guid}/featured-products")]
    public async Task<IActionResult> AddFeaturedProduct(Guid id, [FromBody] AddFeaturedProductRequest request)
    {
        await _mediator.Send(new AddFeaturedProductCommand(id, request.ProductId));
        return NoContent();
    }

    [HttpPut("{id:guid}/seo")]
    public async Task<IActionResult> SetSeo(Guid id, [FromBody] SetSeoRequest request)
    {
        await _mediator.Send(new SetCategorySeoCommand(id, request.MetaTitle, request.MetaDescription, request.MetaKeywords));
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _mediator.Send(new ActivateCategoryCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _mediator.Send(new DeactivateCategoryCommand(id));
        return NoContent();
    }
}

public record CreateRootCategoryRequest(string Name, string? Description = null, string? ImageUrl = null);
public record CreateChildCategoryRequest(string Name, Guid ParentId, int Level, string? Description = null);
public record UpdateCategoryRequest(string Name, string? Description, string? ImageUrl, string? IconUrl);
public record AddAttributeRequest(string Name, AttributeType Type, bool IsFilterable = true, bool IsSearchable = false);
public record AddFeaturedProductRequest(Guid ProductId);
public record SetSeoRequest(string? MetaTitle, string? MetaDescription, string? MetaKeywords);
