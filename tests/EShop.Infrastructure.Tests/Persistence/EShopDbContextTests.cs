using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Common;
using EShop.Domain.ValueObjects;
using EShop.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for EShopDbContext
/// Uses in-memory database for testing
/// </summary>
public class EShopDbContextTests : IDisposable
{
    private readonly EShopDbContext _context;
    private readonly Mock<IMediator> _mediatorMock;

    public EShopDbContextTests()
    {
        _mediatorMock = new Mock<IMediator>();
        
        var options = new DbContextOptionsBuilder<EShopDbContext>()
            .UseInMemoryDatabase(databaseName: $"EShopTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new EShopDbContext(options, _mediatorMock.Object);
    }

    private static Order CreateTestOrder()
    {
        var customerId = Guid.NewGuid();
        var address = new Address("123 Test St", "Test City", "Test Country", "12345");
        var order = Order.Create(customerId, address, "USD");
        return order;
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem(Guid.NewGuid(), "Test Product", Money.FromUSD(100), 2);

        // Act
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Assert
        var savedOrder = await _context.Orders.FindAsync(order.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.CustomerId.Should().Be(order.CustomerId);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldDispatchDomainEvents()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem(Guid.NewGuid(), "Test Product", Money.FromUSD(100), 1);
        order.Place();

        _context.Orders.Add(order);

        // Act
        await _context.SaveChangesAsync();

        // Assert - Domain events should be published via IPublisher interface
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldClearDomainEventsAfterPublishing()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem(Guid.NewGuid(), "Test Product", Money.FromUSD(100), 1);
        
        _context.Orders.Add(order);

        // Act
        await _context.SaveChangesAsync();

        // Assert - Events should be cleared after save
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Orders_ShouldSupportQueryingByStatus()
    {
        // Arrange
        var order1 = CreateTestOrder();
        order1.AddItem(Guid.NewGuid(), "Product", Money.FromUSD(50), 1);
        
        var order2 = CreateTestOrder();
        order2.AddItem(Guid.NewGuid(), "Product", Money.FromUSD(75), 1);
        order2.Place();

        _context.Orders.AddRange(order1, order2);
        await _context.SaveChangesAsync();

        // Act
        var pendingOrders = await _context.Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync();
        
        var placedOrders = await _context.Orders
            .Where(o => o.Status == OrderStatus.Placed)
            .ToListAsync();

        // Assert
        pendingOrders.Should().ContainSingle();
        placedOrders.Should().ContainSingle();
    }

    [Fact]
    public async Task Orders_ShouldSupportQueryingByCustomerId()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var address = new Address("123 Main St", "City", "Country", "12345");
        
        var order1 = Order.Create(customerId, address, "USD");
        order1.AddItem(Guid.NewGuid(), "Product", Money.FromUSD(50), 1);
        
        var order2 = Order.Create(customerId, address, "USD");
        order2.AddItem(Guid.NewGuid(), "Product", Money.FromUSD(75), 1);
        
        var otherOrder = CreateTestOrder();
        otherOrder.AddItem(Guid.NewGuid(), "Product", Money.FromUSD(100), 1);

        _context.Orders.AddRange(order1, order2, otherOrder);
        await _context.SaveChangesAsync();

        // Act
        var customerOrders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();

        // Assert
        customerOrders.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
