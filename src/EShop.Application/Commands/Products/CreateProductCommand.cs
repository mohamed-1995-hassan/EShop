using EShop.Domain.Aggregates.ProductAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Products;

public record CreateProductCommand(
    string Sku,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int InitialStock,
    int LowStockThreshold,
    List<string>? Categories = null) : IRequest<CreateProductResult>;

public record CreateProductResult(
    bool Success,
    Guid? ProductId,
    string? ErrorMessage);

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, CreateProductResult>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if SKU already exists
            var existing = await _productRepository.GetBySkuAsync(request.Sku, cancellationToken);
            if (existing != null)
                return new CreateProductResult(false, null, $"Product with SKU '{request.Sku}' already exists");

            var price = new Money(request.Price, request.Currency);
            var product = Product.Create(
                request.Sku,
                request.Name,
                request.Description,
                price,
                request.InitialStock,
                request.LowStockThreshold);

            // Add categories
            if (request.Categories != null)
            {
                foreach (var category in request.Categories)
                {
                    product.AddCategory(category);
                }
            }

            await _productRepository.AddAsync(product, cancellationToken);

            return new CreateProductResult(true, product.Id, null);
        }
        catch (Exception ex)
        {
            return new CreateProductResult(false, null, ex.Message);
        }
    }
}
