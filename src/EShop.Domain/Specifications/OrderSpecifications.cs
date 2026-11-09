using System.Linq.Expressions;
using EShop.Domain.Aggregates.OrderAggregate;

namespace EShop.Domain.Specifications;

/// <summary>
/// Order-specific specifications for complex queries
/// </summary>
public class OrderByCustomerSpec : BaseSpecification<Order>
{
    private readonly Guid _customerId;

    public OrderByCustomerSpec(Guid customerId)
    {
        _customerId = customerId;
        AddInclude(o => o.OrderLines);
        ApplyOrderByDescending(o => o.OrderDate);
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.CustomerId == _customerId;
    }
}

public class OrderByStatusSpec : Specification<Order>
{
    private readonly OrderStatus _status;

    public OrderByStatusSpec(OrderStatus status)
    {
        _status = status;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status == _status;
    }
}

public class OrderInDateRangeSpec : Specification<Order>
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public OrderInDateRangeSpec(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.OrderDate >= _startDate && order.OrderDate <= _endDate;
    }
}

public class OrderWithMinimumAmountSpec : Specification<Order>
{
    private readonly decimal _minimumAmount;

    public OrderWithMinimumAmountSpec(decimal minimumAmount)
    {
        _minimumAmount = minimumAmount;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.TotalAmount.Amount >= _minimumAmount;
    }
}

public class PendingOrdersSpec : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status == OrderStatus.Pending || 
                        order.Status == OrderStatus.Placed;
    }
}

public class CompletedOrdersSpec : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status == OrderStatus.Completed;
    }
}

public class CancellableOrdersSpec : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status != OrderStatus.Shipped &&
                        order.Status != OrderStatus.Completed &&
                        order.Status != OrderStatus.Cancelled;
    }
}

public class OrdersReadyForShippingSpec : BaseSpecification<Order>
{
    public OrdersReadyForShippingSpec()
    {
        AddInclude(o => o.OrderLines);
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status == OrderStatus.Confirmed;
    }
}

public class OrderContainingProductSpec : Specification<Order>
{
    private readonly Guid _productId;

    public OrderContainingProductSpec(Guid productId)
    {
        _productId = productId;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.OrderLines.Any(ol => ol.ProductId == _productId);
    }
}

public class HighValueOrdersSpec : Specification<Order>
{
    private readonly decimal _threshold;

    public HighValueOrdersSpec(decimal threshold = 500)
    {
        _threshold = threshold;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.TotalAmount.Amount >= _threshold;
    }
}

/// <summary>
/// Paged orders specification
/// </summary>
public class PagedOrdersSpec : BaseSpecification<Order>
{
    public PagedOrdersSpec(int pageNumber, int pageSize, OrderStatus? status = null)
    {
        if (status.HasValue)
        {
            // Will be combined with status filter
        }

        AddInclude(o => o.OrderLines);
        ApplyOrderByDescending(o => o.OrderDate);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => true; // Base - combine with other specs
    }
}

/// <summary>
/// Composite specification for dashboard queries
/// </summary>
public static class OrderSpecificationBuilder
{
    public static Specification<Order> ForCustomerDashboard(
        Guid customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        OrderStatus? status = null)
    {
        Specification<Order> spec = new OrderByCustomerSpec(customerId);

        if (fromDate.HasValue && toDate.HasValue)
        {
            spec = spec.And(new OrderInDateRangeSpec(fromDate.Value, toDate.Value));
        }

        if (status.HasValue)
        {
            spec = spec.And(new OrderByStatusSpec(status.Value));
        }

        return spec;
    }

    public static Specification<Order> ForFulfillmentQueue()
    {
        return new OrderByStatusSpec(OrderStatus.Confirmed);
    }

    public static Specification<Order> ForSalesReport(DateTime startDate, DateTime endDate)
    {
        return new OrderInDateRangeSpec(startDate, endDate)
            .And(new CompletedOrdersSpec());
    }

    public static Specification<Order> ForHighValueCustomerOrders(Guid customerId, decimal threshold)
    {
        return new OrderByCustomerSpec(customerId)
            .And(new HighValueOrdersSpec(threshold));
    }
}
