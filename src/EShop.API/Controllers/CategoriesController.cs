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
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCatalogCategoryRequest request)
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

/// <summary>Request to create a root category</summary>
public class CreateRootCategoryRequest
{
    /// <summary>Category name</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Category description</summary>
    public string? Description { get; set; }
    /// <summary>Image URL</summary>
    public string? ImageUrl { get; set; }
}

/// <summary>Request to create a child category</summary>
public class CreateChildCategoryRequest
{
    /// <summary>Category name</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Parent category ID</summary>
    public Guid ParentId { get; set; }
    /// <summary>Nesting level</summary>
    public int Level { get; set; }
    /// <summary>Category description</summary>
    public string? Description { get; set; }
}

/// <summary>Request to update a category</summary>
public class UpdateCatalogCategoryRequest
{
    /// <summary>Category name</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Category description</summary>
    public string? Description { get; set; }
    /// <summary>Image URL</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Icon URL</summary>
    public string? IconUrl { get; set; }
}

/// <summary>Request to add a category attribute</summary>
public class AddAttributeRequest
{
    /// <summary>Attribute name</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Attribute type</summary>
    public AttributeType Type { get; set; }
    /// <summary>Whether attribute is filterable</summary>
    public bool IsFilterable { get; set; } = true;
    /// <summary>Whether attribute is searchable</summary>
    public bool IsSearchable { get; set; } = false;
}

/// <summary>Request to add a featured product</summary>
public class AddFeaturedProductRequest
{
    /// <summary>Product ID to feature</summary>
    public Guid ProductId { get; set; }
}

/// <summary>Request to set SEO metadata</summary>
public class SetSeoRequest
{
    /// <summary>Meta title</summary>
    public string? MetaTitle { get; set; }
    /// <summary>Meta description</summary>
    public string? MetaDescription { get; set; }
    /// <summary>Meta keywords</summary>
    public string? MetaKeywords { get; set; }
}
