using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Products;

public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    decimal? NewPrice = null,
    string Currency = "KWD") : IRequest<UpdateProductResult>;

public record UpdateProductResult(bool Success, string? ErrorMessage);

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, UpdateProductResult>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
                return new UpdateProductResult(false, "Product not found");

            product.UpdateDetails(request.Name, request.Description);

            if (request.NewPrice.HasValue)
            {
                product.UpdatePrice(new Money(request.NewPrice.Value, request.Currency));
            }

            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync(cancellationToken);

            return new UpdateProductResult(true, null);
        }
        catch (Exception ex)
        {
            return new UpdateProductResult(false, ex.Message);
        }
    }
}

// ============================================
// ADD STOCK COMMAND
// ============================================
public record AddStockCommand(
    Guid ProductId,
    int Quantity,
    string? Reference = null) : IRequest<AddStockResult>;

public record AddStockResult(bool Success, int NewStockLevel, string? ErrorMessage);

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, AddStockResult>
{
    private readonly IProductRepository _productRepository;

    public AddStockCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<AddStockResult> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
                return new AddStockResult(false, 0, "Product not found");

            product.AddStock(request.Quantity, request.Reference);
            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync(cancellationToken);

            return new AddStockResult(true, product.StockQuantity, null);
        }
        catch (Exception ex)
        {
            return new AddStockResult(false, 0, ex.Message);
        }
    }
}

// ============================================
// DISCONTINUE PRODUCT COMMAND
// ============================================
public record DiscontinueProductCommand(Guid ProductId) : IRequest<DiscontinueProductResult>;

public record DiscontinueProductResult(bool Success, string? ErrorMessage);

public class DiscontinueProductCommandHandler : IRequestHandler<DiscontinueProductCommand, DiscontinueProductResult>
{
    private readonly IProductRepository _productRepository;

    public DiscontinueProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<DiscontinueProductResult> Handle(DiscontinueProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
                return new DiscontinueProductResult(false, "Product not found");

            product.Discontinue();
            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync(cancellationToken);

            return new DiscontinueProductResult(true, null);
        }
        catch (Exception ex)
        {
            return new DiscontinueProductResult(false, ex.Message);
        }
    }
}
