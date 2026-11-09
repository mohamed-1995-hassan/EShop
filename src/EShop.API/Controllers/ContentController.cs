using EShop.Application.Commands.Content;
using EShop.Application.Queries.Content;
using EShop.Domain.Aggregates.ContentAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentController(IMediator mediator) => _mediator = mediator;

    // Banner endpoints
    [HttpGet("banners/{id:guid}")]
    public async Task<ActionResult<BannerDto>> GetBanner(Guid id)
    {
        var banner = await _mediator.Send(new GetBannerQuery(id));
        return banner != null ? Ok(banner) : NotFound();
    }

    [HttpGet("banners/active")]
    public async Task<ActionResult<IReadOnlyList<BannerDto>>> GetActiveBanners([FromQuery] BannerPosition position)
    {
        var banners = await _mediator.Send(new GetActiveBannersQuery(position));
        return Ok(banners);
    }

    [HttpPost("banners")]
    public async Task<ActionResult<Guid>> CreateBanner([FromBody] CreateBannerRequest request)
    {
        var id = await _mediator.Send(new CreateBannerCommand(request.Name, request.Title, request.ImageUrl, request.Position, request.LinkUrl));
        return CreatedAtAction(nameof(GetBanner), new { id }, id);
    }

    [HttpPut("banners/{id:guid}")]
    public async Task<IActionResult> UpdateBanner(Guid id, [FromBody] UpdateBannerRequest request)
    {
        await _mediator.Send(new UpdateBannerCommand(id, request.Title, request.Subtitle, request.ImageUrl, request.MobileImageUrl, request.LinkUrl));
        return NoContent();
    }

    [HttpPut("banners/{id:guid}/schedule")]
    public async Task<IActionResult> ScheduleBanner(Guid id, [FromBody] ScheduleBannerRequest request)
    {
        await _mediator.Send(new ScheduleBannerCommand(id, request.StartDate, request.EndDate));
        return NoContent();
    }

    [HttpPost("banners/{id:guid}/publish")]
    public async Task<IActionResult> PublishBanner(Guid id)
    {
        await _mediator.Send(new PublishBannerCommand(id));
        return NoContent();
    }

    [HttpPost("banners/{id:guid}/unpublish")]
    public async Task<IActionResult> UnpublishBanner(Guid id)
    {
        await _mediator.Send(new UnpublishBannerCommand(id));
        return NoContent();
    }

    [HttpPost("banners/{id:guid}/click")]
    public async Task<IActionResult> RecordBannerClick(Guid id)
    {
        await _mediator.Send(new RecordBannerClickCommand(id));
        return NoContent();
    }

    // Landing Page endpoints
    [HttpGet("pages/{id:guid}")]
    public async Task<ActionResult<LandingPageDto>> GetPage(Guid id)
    {
        var page = await _mediator.Send(new GetLandingPageQuery(id));
        return page != null ? Ok(page) : NotFound();
    }

    [HttpGet("pages/slug/{slug}")]
    public async Task<ActionResult<LandingPageDto>> GetPageBySlug(string slug)
    {
        var page = await _mediator.Send(new GetLandingPageBySlugQuery(slug));
        return page != null ? Ok(page) : NotFound();
    }

    [HttpPost("pages")]
    public async Task<ActionResult<Guid>> CreatePage([FromBody] CreatePageRequest request)
    {
        var id = await _mediator.Send(new CreateLandingPageCommand(request.Title, request.Slug, request.TemplateId));
        return CreatedAtAction(nameof(GetPage), new { id }, id);
    }

    [HttpPost("pages/{id:guid}/blocks")]
    public async Task<ActionResult<Guid>> AddBlock(Guid id, [FromBody] AddBlockRequest request)
    {
        var blockId = await _mediator.Send(new AddContentBlockCommand(id, request.Type, request.Content, request.Position));
        return Ok(blockId);
    }

    [HttpPost("pages/{id:guid}/publish")]
    public async Task<IActionResult> PublishPage(Guid id)
    {
        await _mediator.Send(new PublishLandingPageCommand(id));
        return NoContent();
    }

    // FAQ endpoints
    [HttpGet("faq")]
    public async Task<ActionResult<IReadOnlyList<FaqCategoryDto>>> GetFaqCategories()
    {
        var categories = await _mediator.Send(new GetFaqCategoriesQuery());
        return Ok(categories);
    }

    [HttpPost("faq/categories")]
    public async Task<ActionResult<Guid>> CreateFaqCategory([FromBody] CreateFaqCategoryRequest request)
    {
        var id = await _mediator.Send(new CreateFaqCategoryCommand(request.Name, request.Description));
        return Ok(id);
    }

    [HttpPost("faq/categories/{categoryId:guid}/items")]
    public async Task<ActionResult<Guid>> AddFaqItem(Guid categoryId, [FromBody] AddFaqItemRequest request)
    {
        var id = await _mediator.Send(new AddFaqItemCommand(categoryId, request.Question, request.Answer));
        return Ok(id);
    }

    [HttpPost("faq/categories/{categoryId:guid}/items/{itemId:guid}/helpful")]
    public async Task<IActionResult> MarkFaqHelpful(Guid categoryId, Guid itemId, [FromBody] MarkHelpfulRequest request)
    {
        await _mediator.Send(new MarkFaqHelpfulCommand(categoryId, itemId, request.Helpful));
        return NoContent();
    }

    // Blog endpoints
    [HttpGet("blog")]
    public async Task<ActionResult<IReadOnlyList<BlogPostDto>>> GetBlogPosts([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var posts = await _mediator.Send(new GetBlogPostsQuery(skip, take));
        return Ok(posts);
    }

    [HttpGet("blog/slug/{slug}")]
    public async Task<ActionResult<BlogPostDto>> GetBlogPostBySlug(string slug)
    {
        var post = await _mediator.Send(new GetBlogPostBySlugQuery(slug));
        return post != null ? Ok(post) : NotFound();
    }

    [HttpGet("blog/featured")]
    public async Task<ActionResult<IReadOnlyList<BlogPostDto>>> GetFeaturedPosts([FromQuery] int count = 5)
    {
        var posts = await _mediator.Send(new GetFeaturedBlogPostsQuery(count));
        return Ok(posts);
    }

    [HttpPost("blog")]
    public async Task<ActionResult<Guid>> CreateBlogPost([FromBody] CreateBlogPostRequest request)
    {
        var id = await _mediator.Send(new CreateBlogPostCommand(request.Title, request.Content, request.AuthorId, request.AuthorName));
        return Ok(id);
    }

    [HttpPut("blog/{id:guid}")]
    public async Task<IActionResult> UpdateBlogPost(Guid id, [FromBody] UpdateBlogPostRequest request)
    {
        await _mediator.Send(new UpdateBlogPostCommand(id, request.Title, request.Excerpt, request.Content, request.FeaturedImageUrl));
        return NoContent();
    }

    [HttpPost("blog/{id:guid}/publish")]
    public async Task<IActionResult> PublishBlogPost(Guid id)
    {
        await _mediator.Send(new PublishBlogPostCommand(id));
        return NoContent();
    }

    [HttpPut("blog/{id:guid}/tags")]
    public async Task<IActionResult> SetBlogTags(Guid id, [FromBody] SetTagsRequest request)
    {
        await _mediator.Send(new SetBlogPostTagsCommand(id, request.Tags));
        return NoContent();
    }
}

// Banner requests
public record CreateBannerRequest(string Name, string Title, string ImageUrl, BannerPosition Position, string? LinkUrl = null);
public record UpdateBannerRequest(string Title, string? Subtitle, string ImageUrl, string? MobileImageUrl, string? LinkUrl);
public record ScheduleBannerRequest(DateTime? StartDate, DateTime? EndDate);

// Page requests
public record CreatePageRequest(string Title, string Slug, string? TemplateId = null);
public record AddBlockRequest(BlockType Type, string Content, int Position);

// FAQ requests
public record CreateFaqCategoryRequest(string Name, string? Description = null);
public record AddFaqItemRequest(string Question, string Answer);
public record MarkHelpfulRequest(bool Helpful);

// Blog requests
public record CreateBlogPostRequest(string Title, string Content, Guid AuthorId, string AuthorName);
public record UpdateBlogPostRequest(string Title, string Excerpt, string Content, string? FeaturedImageUrl);
public record SetTagsRequest(string[] Tags);
