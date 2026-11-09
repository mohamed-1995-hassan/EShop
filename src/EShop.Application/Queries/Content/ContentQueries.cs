using EShop.Domain.Aggregates.ContentAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Content;

public record BannerDto(Guid Id, string Name, string Title, string? Subtitle, string ImageUrl, string? LinkUrl, BannerPosition Position, BannerStatus Status, int ClickCount, int ImpressionCount, decimal ClickThroughRate, bool IsActive);
public record LandingPageDto(Guid Id, string Title, string Slug, PageStatus Status, int ViewCount, DateTime? PublishedAt);
public record FaqCategoryDto(Guid Id, string Name, string? Description, bool IsActive, IReadOnlyList<FaqItemDto> Items);
public record FaqItemDto(Guid Id, string Question, string Answer, int ViewCount, int HelpfulCount, int NotHelpfulCount);
public record BlogPostDto(Guid Id, string Title, string Slug, string Excerpt, string AuthorName, BlogPostStatus Status, bool IsFeatured, int ViewCount, DateTime? PublishedAt, IReadOnlyList<string> Tags);

public record GetBannerQuery(Guid Id) : IRequest<BannerDto?>;
public record GetActiveBannersQuery(BannerPosition Position) : IRequest<IReadOnlyList<BannerDto>>;
public record GetLandingPageQuery(Guid Id) : IRequest<LandingPageDto?>;
public record GetLandingPageBySlugQuery(string Slug) : IRequest<LandingPageDto?>;
public record GetFaqCategoriesQuery : IRequest<IReadOnlyList<FaqCategoryDto>>;
public record GetBlogPostsQuery(int Skip = 0, int Take = 10) : IRequest<IReadOnlyList<BlogPostDto>>;
public record GetBlogPostBySlugQuery(string Slug) : IRequest<BlogPostDto?>;
public record GetFeaturedBlogPostsQuery(int Count = 5) : IRequest<IReadOnlyList<BlogPostDto>>;

public class GetBannerHandler : IRequestHandler<GetBannerQuery, BannerDto?>
{
    private readonly IBannerRepository _repo;
    public GetBannerHandler(IBannerRepository repo) => _repo = repo;

    public async Task<BannerDto?> Handle(GetBannerQuery request, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(request.Id, ct);
        return b == null ? null : new BannerDto(b.Id, b.Name, b.Title, b.Subtitle, b.ImageUrl, b.LinkUrl, b.Position, b.Status, b.ClickCount, b.ImpressionCount, b.ClickThroughRate, b.IsActive);
    }
}

public class GetActiveBannersHandler : IRequestHandler<GetActiveBannersQuery, IReadOnlyList<BannerDto>>
{
    private readonly IBannerRepository _repo;
    public GetActiveBannersHandler(IBannerRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<BannerDto>> Handle(GetActiveBannersQuery request, CancellationToken ct)
    {
        var banners = await _repo.GetActiveByPositionAsync(request.Position, ct);
        return banners.Select(b => new BannerDto(b.Id, b.Name, b.Title, b.Subtitle, b.ImageUrl, b.LinkUrl, b.Position, b.Status, b.ClickCount, b.ImpressionCount, b.ClickThroughRate, b.IsActive)).ToList();
    }
}

public class GetLandingPageHandler : IRequestHandler<GetLandingPageQuery, LandingPageDto?>
{
    private readonly ILandingPageRepository _repo;
    public GetLandingPageHandler(ILandingPageRepository repo) => _repo = repo;

    public async Task<LandingPageDto?> Handle(GetLandingPageQuery request, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(request.Id, ct);
        return p == null ? null : new LandingPageDto(p.Id, p.Title, p.Slug, p.Status, p.ViewCount, p.PublishedAt);
    }
}

public class GetLandingPageBySlugHandler : IRequestHandler<GetLandingPageBySlugQuery, LandingPageDto?>
{
    private readonly ILandingPageRepository _repo;
    public GetLandingPageBySlugHandler(ILandingPageRepository repo) => _repo = repo;

    public async Task<LandingPageDto?> Handle(GetLandingPageBySlugQuery request, CancellationToken ct)
    {
        var p = await _repo.GetBySlugAsync(request.Slug, ct);
        return p == null ? null : new LandingPageDto(p.Id, p.Title, p.Slug, p.Status, p.ViewCount, p.PublishedAt);
    }
}

public class GetFaqCategoriesHandler : IRequestHandler<GetFaqCategoriesQuery, IReadOnlyList<FaqCategoryDto>>
{
    private readonly IFaqRepository _repo;
    public GetFaqCategoriesHandler(IFaqRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<FaqCategoryDto>> Handle(GetFaqCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _repo.GetAllActiveAsync(ct);
        return categories.Select(c => new FaqCategoryDto(c.Id, c.Name, c.Description, c.IsActive, c.Items.Select(i => new FaqItemDto(i.Id, i.Question, i.Answer, i.ViewCount, i.HelpfulCount, i.NotHelpfulCount)).ToList())).ToList();
    }
}

public class GetBlogPostsHandler : IRequestHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>
{
    private readonly IBlogPostRepository _repo;
    public GetBlogPostsHandler(IBlogPostRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<BlogPostDto>> Handle(GetBlogPostsQuery request, CancellationToken ct)
    {
        var posts = await _repo.GetPublishedAsync(request.Skip, request.Take, ct);
        return posts.Select(p => new BlogPostDto(p.Id, p.Title, p.Slug, p.Excerpt, p.AuthorName, p.Status, p.IsFeatured, p.ViewCount, p.PublishedAt, p.Tags.ToList())).ToList();
    }
}

public class GetBlogPostBySlugHandler : IRequestHandler<GetBlogPostBySlugQuery, BlogPostDto?>
{
    private readonly IBlogPostRepository _repo;
    public GetBlogPostBySlugHandler(IBlogPostRepository repo) => _repo = repo;

    public async Task<BlogPostDto?> Handle(GetBlogPostBySlugQuery request, CancellationToken ct)
    {
        var p = await _repo.GetBySlugAsync(request.Slug, ct);
        return p == null ? null : new BlogPostDto(p.Id, p.Title, p.Slug, p.Excerpt, p.AuthorName, p.Status, p.IsFeatured, p.ViewCount, p.PublishedAt, p.Tags.ToList());
    }
}

public class GetFeaturedBlogPostsHandler : IRequestHandler<GetFeaturedBlogPostsQuery, IReadOnlyList<BlogPostDto>>
{
    private readonly IBlogPostRepository _repo;
    public GetFeaturedBlogPostsHandler(IBlogPostRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<BlogPostDto>> Handle(GetFeaturedBlogPostsQuery request, CancellationToken ct)
    {
        var posts = await _repo.GetFeaturedAsync(request.Count, ct);
        return posts.Select(p => new BlogPostDto(p.Id, p.Title, p.Slug, p.Excerpt, p.AuthorName, p.Status, p.IsFeatured, p.ViewCount, p.PublishedAt, p.Tags.ToList())).ToList();
    }
}
