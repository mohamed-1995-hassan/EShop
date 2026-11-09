using EShop.Domain.Aggregates.CatalogAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Catalog;

public record CreateRootCategoryCommand(string Name, string? Description = null, string? ImageUrl = null) : IRequest<Guid>;
public record CreateChildCategoryCommand(string Name, Guid ParentId, int Level, string? Description = null) : IRequest<Guid>;
public record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description, string? ImageUrl, string? IconUrl) : IRequest;
public record AddCategoryAttributeCommand(Guid CategoryId, string Name, AttributeType Type, bool IsFilterable = true, bool IsSearchable = false) : IRequest<Guid>;
public record AddFeaturedProductCommand(Guid CategoryId, Guid ProductId) : IRequest;
public record SetCategorySeoCommand(Guid CategoryId, string? MetaTitle, string? MetaDescription, string? MetaKeywords) : IRequest;
public record ActivateCategoryCommand(Guid CategoryId) : IRequest;
public record DeactivateCategoryCommand(Guid CategoryId) : IRequest;

public class CreateRootCategoryHandler : IRequestHandler<CreateRootCategoryCommand, Guid>
{
    private readonly ICategoryRepository _repo;
    public CreateRootCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateRootCategoryCommand request, CancellationToken ct)
    {
        var category = Category.CreateRoot(request.Name, request.Description, request.ImageUrl);
        await _repo.AddAsync(category, ct);
        return category.Id;
    }
}

public class CreateChildCategoryHandler : IRequestHandler<CreateChildCategoryCommand, Guid>
{
    private readonly ICategoryRepository _repo;
    public CreateChildCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateChildCategoryCommand request, CancellationToken ct)
    {
        var parent = await _repo.GetByIdAsync(request.ParentId, ct) ?? throw new InvalidOperationException("Parent category not found");
        var category = Category.CreateChild(request.Name, request.ParentId, request.Level, request.Description);
        parent.AddChildCategory(category.Id);
        await _repo.AddAsync(category, ct);
        await _repo.UpdateAsync(parent, ct);
        return category.Id;
    }
}

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly ICategoryRepository _repo;
    public UpdateCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        category.Update(request.Name, request.Description, request.ImageUrl, request.IconUrl);
        await _repo.UpdateAsync(category, ct);
    }
}

public class AddCategoryAttributeHandler : IRequestHandler<AddCategoryAttributeCommand, Guid>
{
    private readonly ICategoryRepository _repo;
    public AddCategoryAttributeHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddCategoryAttributeCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        var attribute = category.AddAttribute(request.Name, request.Type, request.IsFilterable, request.IsSearchable);
        await _repo.UpdateAsync(category, ct);
        return attribute.Id;
    }
}

public class AddFeaturedProductHandler : IRequestHandler<AddFeaturedProductCommand>
{
    private readonly ICategoryRepository _repo;
    public AddFeaturedProductHandler(ICategoryRepository repo) => _repo = repo;

    public async Task Handle(AddFeaturedProductCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        category.AddFeaturedProduct(request.ProductId);
        await _repo.UpdateAsync(category, ct);
    }
}

public class SetCategorySeoHandler : IRequestHandler<SetCategorySeoCommand>
{
    private readonly ICategoryRepository _repo;
    public SetCategorySeoHandler(ICategoryRepository repo) => _repo = repo;

    public async Task Handle(SetCategorySeoCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        category.SetSeoMetadata(request.MetaTitle, request.MetaDescription, request.MetaKeywords);
        await _repo.UpdateAsync(category, ct);
    }
}

public class ActivateCategoryHandler : IRequestHandler<ActivateCategoryCommand>
{
    private readonly ICategoryRepository _repo;
    public ActivateCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task Handle(ActivateCategoryCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        category.Activate();
        await _repo.UpdateAsync(category, ct);
    }
}

public class DeactivateCategoryHandler : IRequestHandler<DeactivateCategoryCommand>
{
    private readonly ICategoryRepository _repo;
    public DeactivateCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task Handle(DeactivateCategoryCommand request, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(request.CategoryId, ct) ?? throw new InvalidOperationException("Category not found");
        category.Deactivate();
        await _repo.UpdateAsync(category, ct);
    }
}
