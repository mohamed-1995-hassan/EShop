using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.ValueObjects;
using EShop.Infrastructure.Repositories;

namespace EShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Integration tests for InMemoryOrderRepository
/// </summary>
public class InMemoryOrderRepositoryTests
{
    private readonly InMemoryOrderRepository _repository;

    public InMemoryOrderRepositoryTests()
    {
        _repository = new InMemoryOrderRepository();
    }

    private static Order CreateTestOrder()
    {
        var customerId = Guid.NewGuid();
        var address = new Address("123 Test St", "Test City", "Test Country", "12345");
        var order = Order.Create(customerId, address, "USD");
        order.AddItem(Guid.NewGuid(), "Test Product", Money.FromUSD(100), 1);
        return order;
    }

    [Fact]
    public async Task AddAsync_ShouldAddOrder()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        await _repository.AddAsync(order);

        // Assert
        var retrieved = await _repository.GetByIdAsync(order.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        await _repository.AddAsync(order);

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be(order.CustomerId);
        result.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnCustomerOrders()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var address = new Address("123 Test St", "Test City", "Test Country", "12345");
        
        var order1 = Order.Create(customerId, address, "USD");
        order1.AddItem(Guid.NewGuid(), "Product 1", Money.FromUSD(50), 1);
        
        var order2 = Order.Create(customerId, address, "USD");
        order2.AddItem(Guid.NewGuid(), "Product 2", Money.FromUSD(75), 1);
        
        var otherCustomerOrder = CreateTestOrder(); // Different customer

        await _repository.AddAsync(order1);
        await _repository.AddAsync(order2);
        await _repository.AddAsync(otherCustomerOrder);

        // Act
        var result = await _repository.GetByCustomerIdAsync(customerId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.CustomerId.Should().Be(customerId));
    }

    [Fact]
    public async Task GetByCustomerIdAsync_WithNoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByCustomerIdAsync(customerId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_MultipleOrders_ShouldStoreAll()
    {
        // Arrange
        var repository = new InMemoryOrderRepository(); // Fresh instance
        var order1 = CreateTestOrder();
        var order2 = CreateTestOrder();
        var order3 = CreateTestOrder();

        // Act
        await repository.AddAsync(order1);
        await repository.AddAsync(order2);
        await repository.AddAsync(order3);

        // Assert
        var retrieved1 = await repository.GetByIdAsync(order1.Id);
        var retrieved2 = await repository.GetByIdAsync(order2.Id);
        var retrieved3 = await repository.GetByIdAsync(order3.Id);
        
        retrieved1.Should().NotBeNull();
        retrieved2.Should().NotBeNull();
        retrieved3.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_AfterOrderStateChange_ShouldReturnUpdatedState()
    {
        // Arrange
        var order = CreateTestOrder();
        await _repository.AddAsync(order);
        
        // Act - Change state externally
        order.Place();
        
        // Assert - Repository returns the same object reference
        var retrieved = await _repository.GetByIdAsync(order.Id);
        retrieved!.Status.Should().Be(OrderStatus.Placed);
    }
}
