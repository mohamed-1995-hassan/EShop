using System.Linq.Expressions;
using EShop.Domain.Aggregates.ProductAggregate;

namespace EShop.Domain.Specifications;

/// <summary>
/// Product-specific specifications for queries
/// </summary>
public class ProductBySkuSpec : Specification<Product>
{
    private readonly string _sku;

    public ProductBySkuSpec(string sku)
    {
        _sku = sku.ToUpperInvariant();
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.Sku == _sku;
    }
}

public class ProductInStockSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.StockQuantity > 0 && !product.IsDiscontinued;
    }
}

public class ProductLowStockSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.StockQuantity > 0 && 
                          product.StockQuantity <= product.LowStockThreshold &&
                          !product.IsDiscontinued;
    }
}

public class ProductOutOfStockSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.StockQuantity == 0 && !product.IsDiscontinued;
    }
}

public class ProductDiscontinuedSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.IsDiscontinued;
    }
}

public class ProductActiveSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => !product.IsDiscontinued;
    }
}

public class ProductByCategorySpec : Specification<Product>
{
    private readonly string _categoryName;

    public ProductByCategorySpec(string categoryName)
    {
        _categoryName = categoryName;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.Categories.Any(c => 
            c.Name.Equals(_categoryName, StringComparison.OrdinalIgnoreCase));
    }
}

public class ProductPriceRangeSpec : Specification<Product>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public ProductPriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.Price.Amount >= _minPrice && 
                          product.Price.Amount <= _maxPrice;
    }
}

public class ProductSearchSpec : BaseSpecification<Product>
{
    private readonly string _searchTerm;

    public ProductSearchSpec(string searchTerm)
    {
        _searchTerm = searchTerm.ToLowerInvariant();
        AddInclude(p => p.Categories);
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => 
            product.Name.ToLower().Contains(_searchTerm) ||
            product.Description.ToLower().Contains(_searchTerm) ||
            product.Sku.ToLower().Contains(_searchTerm);
    }
}

public class ProductNeedsReorderSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => !product.IsDiscontinued &&
                          product.StockQuantity <= product.LowStockThreshold;
    }
}

public class ProductCreatedAfterSpec : Specification<Product>
{
    private readonly DateTime _date;

    public ProductCreatedAfterSpec(DateTime date)
    {
        _date = date;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.CreatedAt >= _date;
    }
}

public class ProductUpdatedRecentlySpec : Specification<Product>
{
    private readonly int _days;

    public ProductUpdatedRecentlySpec(int days = 7)
    {
        _days = days;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_days);
        return product => product.UpdatedAt.HasValue && product.UpdatedAt.Value >= cutoffDate;
    }
}

/// <summary>
/// Paged products with filtering
/// </summary>
public class PagedProductsSpec : BaseSpecification<Product>
{
    public PagedProductsSpec(
        int pageNumber,
        int pageSize,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool includeOutOfStock = false,
        string? sortBy = null,
        bool descending = false)
    {
        AddInclude(p => p.Categories);

        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            switch (sortBy.ToLower())
            {
                case "name":
                    if (descending) ApplyOrderByDescending(p => p.Name);
                    else ApplyOrderBy(p => p.Name);
                    break;
                case "price":
                    if (descending) ApplyOrderByDescending(p => p.Price.Amount);
                    else ApplyOrderBy(p => p.Price.Amount);
                    break;
                case "created":
                    if (descending) ApplyOrderByDescending(p => p.CreatedAt);
                    else ApplyOrderBy(p => p.CreatedAt);
                    break;
                default:
                    ApplyOrderBy(p => p.Name);
                    break;
            }
        }
        else
        {
            ApplyOrderBy(p => p.Name);
        }

        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => !product.IsDiscontinued;
    }
}

/// <summary>
/// Composite specification builder for products
/// </summary>
public static class ProductSpecificationBuilder
{
    public static Specification<Product> ForStorefront(
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? searchTerm = null)
    {
        Specification<Product> spec = new ProductActiveSpec()
            .And(new ProductInStockSpec());

        if (!string.IsNullOrWhiteSpace(category))
        {
            spec = spec.And(new ProductByCategorySpec(category));
        }

        if (minPrice.HasValue && maxPrice.HasValue)
        {
            spec = spec.And(new ProductPriceRangeSpec(minPrice.Value, maxPrice.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            spec = spec.And(new ProductSearchSpec(searchTerm));
        }

        return spec;
    }

    public static Specification<Product> ForInventoryAlert()
    {
        return new ProductActiveSpec()
            .And(new ProductLowStockSpec().Or(new ProductOutOfStockSpec()));
    }

    public static Specification<Product> ForReorderReport()
    {
        return new ProductNeedsReorderSpec();
    }
}
