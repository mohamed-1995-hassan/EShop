using EShop.Domain.Aggregates.ProductAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly EShopDbContext _dbContext;

    public ProductRepository(EShopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Sku == sku.ToUpperInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(p => p.Categories.Any(c => c.Name == category))
            .Where(p => !p.IsDiscontinued)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(p => !p.IsDiscontinued && p.StockQuantity <= p.LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLowerInvariant();
        return await _dbContext.Products
            .Where(p => p.Name.ToLower().Contains(term) || 
                        p.Sku.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term))
            .Where(p => !p.IsDiscontinued)
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AnyAsync(p => p.Sku == sku.ToUpperInvariant(), cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        _dbContext.Products.Update(product);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesWithOutboxAsync(cancellationToken);
    }
}
