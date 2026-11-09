using EShop.Domain.Aggregates.ContentAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Content;

// Banner Commands
public record CreateBannerCommand(string Name, string Title, string ImageUrl, BannerPosition Position, string? LinkUrl = null) : IRequest<Guid>;
public record UpdateBannerCommand(Guid BannerId, string Title, string? Subtitle, string ImageUrl, string? MobileImageUrl, string? LinkUrl) : IRequest;
public record ScheduleBannerCommand(Guid BannerId, DateTime? StartDate, DateTime? EndDate) : IRequest;
public record PublishBannerCommand(Guid BannerId) : IRequest;
public record UnpublishBannerCommand(Guid BannerId) : IRequest;
public record RecordBannerClickCommand(Guid BannerId) : IRequest;

// Landing Page Commands
public record CreateLandingPageCommand(string Title, string Slug, string? TemplateId = null) : IRequest<Guid>;
public record AddContentBlockCommand(Guid PageId, BlockType Type, string Content, int Position) : IRequest<Guid>;
public record PublishLandingPageCommand(Guid PageId) : IRequest;

// FAQ Commands
public record CreateFaqCategoryCommand(string Name, string? Description = null) : IRequest<Guid>;
public record AddFaqItemCommand(Guid CategoryId, string Question, string Answer) : IRequest<Guid>;
public record MarkFaqHelpfulCommand(Guid CategoryId, Guid ItemId, bool Helpful) : IRequest;

// Blog Post Commands
public record CreateBlogPostCommand(string Title, string Content, Guid AuthorId, string AuthorName) : IRequest<Guid>;
public record UpdateBlogPostCommand(Guid PostId, string Title, string Excerpt, string Content, string? FeaturedImageUrl) : IRequest;
public record PublishBlogPostCommand(Guid PostId) : IRequest;
public record SetBlogPostTagsCommand(Guid PostId, string[] Tags) : IRequest;

// Handlers
public class CreateBannerHandler : IRequestHandler<CreateBannerCommand, Guid>
{
    private readonly IBannerRepository _repo;
    public CreateBannerHandler(IBannerRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateBannerCommand request, CancellationToken ct)
    {
        var banner = Banner.Create(request.Name, request.Title, request.ImageUrl, request.Position, request.LinkUrl);
        await _repo.AddAsync(banner, ct);
        return banner.Id;
    }
}

public class UpdateBannerHandler : IRequestHandler<UpdateBannerCommand>
{
    private readonly IBannerRepository _repo;
    public UpdateBannerHandler(IBannerRepository repo) => _repo = repo;

    public async Task Handle(UpdateBannerCommand request, CancellationToken ct)
    {
        var banner = await _repo.GetByIdAsync(request.BannerId, ct) ?? throw new InvalidOperationException("Banner not found");
        banner.Update(request.Title, request.Subtitle, request.ImageUrl, request.MobileImageUrl, request.LinkUrl);
        await _repo.UpdateAsync(banner, ct);
    }
}

public class ScheduleBannerHandler : IRequestHandler<ScheduleBannerCommand>
{
    private readonly IBannerRepository _repo;
    public ScheduleBannerHandler(IBannerRepository repo) => _repo = repo;

    public async Task Handle(ScheduleBannerCommand request, CancellationToken ct)
    {
        var banner = await _repo.GetByIdAsync(request.BannerId, ct) ?? throw new InvalidOperationException("Banner not found");
        banner.Schedule(request.StartDate, request.EndDate);
        await _repo.UpdateAsync(banner, ct);
    }
}

public class PublishBannerHandler : IRequestHandler<PublishBannerCommand>
{
    private readonly IBannerRepository _repo;
    public PublishBannerHandler(IBannerRepository repo) => _repo = repo;

    public async Task Handle(PublishBannerCommand request, CancellationToken ct)
    {
        var banner = await _repo.GetByIdAsync(request.BannerId, ct) ?? throw new InvalidOperationException("Banner not found");
        banner.Publish();
        await _repo.UpdateAsync(banner, ct);
    }
}

public class UnpublishBannerHandler : IRequestHandler<UnpublishBannerCommand>
{
    private readonly IBannerRepository _repo;
    public UnpublishBannerHandler(IBannerRepository repo) => _repo = repo;

    public async Task Handle(UnpublishBannerCommand request, CancellationToken ct)
    {
        var banner = await _repo.GetByIdAsync(request.BannerId, ct) ?? throw new InvalidOperationException("Banner not found");
        banner.Unpublish();
        await _repo.UpdateAsync(banner, ct);
    }
}

public class RecordBannerClickHandler : IRequestHandler<RecordBannerClickCommand>
{
    private readonly IBannerRepository _repo;
    public RecordBannerClickHandler(IBannerRepository repo) => _repo = repo;

    public async Task Handle(RecordBannerClickCommand request, CancellationToken ct)
    {
        var banner = await _repo.GetByIdAsync(request.BannerId, ct) ?? throw new InvalidOperationException("Banner not found");
        banner.RecordClick();
        await _repo.UpdateAsync(banner, ct);
    }
}

public class CreateLandingPageHandler : IRequestHandler<CreateLandingPageCommand, Guid>
{
    private readonly ILandingPageRepository _repo;
    public CreateLandingPageHandler(ILandingPageRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateLandingPageCommand request, CancellationToken ct)
    {
        var page = LandingPage.Create(request.Title, request.Slug, request.TemplateId);
        await _repo.AddAsync(page, ct);
        return page.Id;
    }
}

public class AddContentBlockHandler : IRequestHandler<AddContentBlockCommand, Guid>
{
    private readonly ILandingPageRepository _repo;
    public AddContentBlockHandler(ILandingPageRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddContentBlockCommand request, CancellationToken ct)
    {
        var page = await _repo.GetByIdAsync(request.PageId, ct) ?? throw new InvalidOperationException("Page not found");
        var block = page.AddBlock(request.Type, request.Content, request.Position);
        await _repo.UpdateAsync(page, ct);
        return block.Id;
    }
}

public class PublishLandingPageHandler : IRequestHandler<PublishLandingPageCommand>
{
    private readonly ILandingPageRepository _repo;
    public PublishLandingPageHandler(ILandingPageRepository repo) => _repo = repo;

    public async Task Handle(PublishLandingPageCommand request, CancellationToken ct)
    {
        var page = await _repo.GetByIdAsync(request.PageId, ct) ?? throw new InvalidOperationException("Page not found");
        page.Publish();
        await _repo.UpdateAsync(page, ct);
    }
}

public class CreateFaqCategoryHandler : IRequestHandler<CreateFaqCategoryCommand, Guid>
{
    private readonly IFaqRepository _repo;
    public CreateFaqCategoryHandler(IFaqRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateFaqCategoryCommand request, CancellationToken ct)
    {
        var category = FaqCategory.Create(request.Name, request.Description);
        await _repo.AddAsync(category, ct);
        return category.Id;
    }
}

public class AddFaqItemHandler : IRequestHandler<AddFaqItemCommand, Guid>
{
    private readonly IFaqRepository _repo;
    public AddFaqItemHandler(IFaqRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddFaqItemCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        var item = category.AddItem(request.Question, request.Answer);
        await _repo.UpdateAsync(category, ct);
        return item.Id;
    }
}

public class MarkFaqHelpfulHandler : IRequestHandler<MarkFaqHelpfulCommand>
{
    private readonly IFaqRepository _repo;
    public MarkFaqHelpfulHandler(IFaqRepository repo) => _repo = repo;

    public async Task Handle(MarkFaqHelpfulCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        var item = category.Items.FirstOrDefault(i => i.Id == request.ItemId) ?? throw new InvalidOperationException("FAQ item not found");
        if (request.Helpful) item.MarkHelpful(); else item.MarkNotHelpful();
        await _repo.UpdateAsync(category, ct);
    }
}

public class CreateBlogPostHandler : IRequestHandler<CreateBlogPostCommand, Guid>
{
    private readonly IBlogPostRepository _repo;
    public CreateBlogPostHandler(IBlogPostRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateBlogPostCommand request, CancellationToken ct)
    {
        var post = BlogPost.Create(request.Title, request.Content, request.AuthorId, request.AuthorName);
        await _repo.AddAsync(post, ct);
        return post.Id;
    }
}

public class UpdateBlogPostHandler : IRequestHandler<UpdateBlogPostCommand>
{
    private readonly IBlogPostRepository _repo;
    public UpdateBlogPostHandler(IBlogPostRepository repo) => _repo = repo;

    public async Task Handle(UpdateBlogPostCommand request, CancellationToken ct)
    {
        var post = await _repo.GetByIdAsync(request.PostId, ct) ?? throw new InvalidOperationException("Post not found");
        post.Update(request.Title, request.Excerpt, request.Content, request.FeaturedImageUrl);
        await _repo.UpdateAsync(post, ct);
    }
}

public class PublishBlogPostHandler : IRequestHandler<PublishBlogPostCommand>
{
    private readonly IBlogPostRepository _repo;
    public PublishBlogPostHandler(IBlogPostRepository repo) => _repo = repo;

    public async Task Handle(PublishBlogPostCommand request, CancellationToken ct)
    {
        var post = await _repo.GetByIdAsync(request.PostId, ct) ?? throw new InvalidOperationException("Post not found");
        post.Publish();
        await _repo.UpdateAsync(post, ct);
    }
}

public class SetBlogPostTagsHandler : IRequestHandler<SetBlogPostTagsCommand>
{
    private readonly IBlogPostRepository _repo;
    public SetBlogPostTagsHandler(IBlogPostRepository repo) => _repo = repo;

    public async Task Handle(SetBlogPostTagsCommand request, CancellationToken ct)
    {
        var post = await _repo.GetByIdAsync(request.PostId, ct) ?? throw new InvalidOperationException("Post not found");
        post.SetTags(request.Tags);
        await _repo.UpdateAsync(post, ct);
    }
}
