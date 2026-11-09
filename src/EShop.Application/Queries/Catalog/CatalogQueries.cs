using EShop.Domain.Aggregates.CatalogAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Catalog;

public record CategoryDto(Guid Id, string Name, string Slug, string? Description, string? ImageUrl, Guid? ParentCategoryId, int Level, bool IsActive, bool IsFeatured, int ProductCount, IReadOnlyList<Guid> ChildCategoryIds);
public record CategorySummaryDto(Guid Id, string Name, string Slug, int ProductCount, bool IsActive, int Level);

public record GetCategoryQuery(Guid Id) : IRequest<CategoryDto?>;
public record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDto?>;
public record GetRootCategoriesQuery : IRequest<IReadOnlyList<CategorySummaryDto>>;
public record GetChildCategoriesQuery(Guid ParentId) : IRequest<IReadOnlyList<CategorySummaryDto>>;
public record GetFeaturedCategoriesQuery : IRequest<IReadOnlyList<CategorySummaryDto>>;

public class GetCategoryHandler : IRequestHandler<GetCategoryQuery, CategoryDto?>
{
    private readonly ICategoryRepository _repo;
    public GetCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<CategoryDto?> Handle(GetCategoryQuery request, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(request.Id, ct);
        return c == null ? null : new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.ParentCategoryId, c.Level, c.IsActive, c.IsFeatured, c.ProductCount, c.ChildCategoryIds.ToList());
    }
}

public class GetCategoryBySlugHandler : IRequestHandler<GetCategoryBySlugQuery, CategoryDto?>
{
    private readonly ICategoryRepository _repo;
    public GetCategoryBySlugHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<CategoryDto?> Handle(GetCategoryBySlugQuery request, CancellationToken ct)
    {
        var c = await _repo.GetBySlugAsync(request.Slug, ct);
        return c == null ? null : new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.ParentCategoryId, c.Level, c.IsActive, c.IsFeatured, c.ProductCount, c.ChildCategoryIds.ToList());
    }
}

public class GetRootCategoriesHandler : IRequestHandler<GetRootCategoriesQuery, IReadOnlyList<CategorySummaryDto>>
{
    private readonly ICategoryRepository _repo;
    public GetRootCategoriesHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CategorySummaryDto>> Handle(GetRootCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _repo.GetRootCategoriesAsync(ct);
        return categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug, c.ProductCount, c.IsActive, c.Level)).ToList();
    }
}

public class GetChildCategoriesHandler : IRequestHandler<GetChildCategoriesQuery, IReadOnlyList<CategorySummaryDto>>
{
    private readonly ICategoryRepository _repo;
    public GetChildCategoriesHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CategorySummaryDto>> Handle(GetChildCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _repo.GetChildrenAsync(request.ParentId, ct);
        return categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug, c.ProductCount, c.IsActive, c.Level)).ToList();
    }
}

public class GetFeaturedCategoriesHandler : IRequestHandler<GetFeaturedCategoriesQuery, IReadOnlyList<CategorySummaryDto>>
{
    private readonly ICategoryRepository _repo;
    public GetFeaturedCategoriesHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CategorySummaryDto>> Handle(GetFeaturedCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _repo.GetFeaturedAsync(ct);
        return categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug, c.ProductCount, c.IsActive, c.Level)).ToList();
    }
}
