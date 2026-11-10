using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Tests.Aggregates;

/// <summary>
/// Unit tests for OrderLine Entity
/// Note: OrderLine is part of the Order aggregate and should only be
/// accessed through the aggregate root. These tests verify the entity behavior.
/// </summary>
public class OrderLineTests
{
    private static Money CreateUnitPrice(decimal amount = 10m) =>
        Money.FromUSD(amount);

    #region Consolidated Tests via Order Aggregate

    [Fact]
    public void OrderLine_ShouldCalculateLineTotalCorrectly()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), 
            new Address("Street", "City", "Country", "12345"), "USD");
        
        // Act
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(15m), 3);

        // Assert
        var line = order.OrderLines.First();
        line.LineTotal.Amount.Should().Be(45m); // 15 * 3
    }

    [Fact]
    public void OrderLine_WhenQuantityIncreased_ShouldRecalculateLineTotal()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), 
            new Address("Street", "City", "Country", "12345"), "USD");
        var productId = Guid.NewGuid();
        
        order.AddItem(productId, "Product", CreateUnitPrice(20m), 2);
        
        // Act - Add same product again (consolidation)
        order.AddItem(productId, "Product", CreateUnitPrice(20m), 3);

        // Assert
        var line = order.OrderLines.First();
        line.Quantity.Should().Be(5);
        line.LineTotal.Amount.Should().Be(100m); // 20 * 5
    }

    [Fact]
    public void OrderLine_ShouldStoreProductInformation()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), 
            new Address("Street", "City", "Country", "12345"), "USD");
        var productId = Guid.NewGuid();
        var unitPrice = CreateUnitPrice(99.99m);

        // Act
        order.AddItem(productId, "Premium Widget", unitPrice, 1);

        // Assert
        var line = order.OrderLines.First();
        line.ProductId.Should().Be(productId);
        line.ProductName.Should().Be("Premium Widget");
        line.UnitPrice.Amount.Should().Be(99.99m);
        line.UnitPrice.Currency.Should().Be("USD");
    }

    [Fact]
    public void OrderLine_ShouldHaveUniqueId()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), 
            new Address("Street", "City", "Country", "12345"), "USD");
        
        // Act
        order.AddItem(Guid.NewGuid(), "Product 1", CreateUnitPrice(), 1);
        order.AddItem(Guid.NewGuid(), "Product 2", CreateUnitPrice(), 1);

        // Assert
        var ids = order.OrderLines.Select(l => l.Id).ToList();
        ids.Should().HaveCount(2);
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().AllSatisfy(id => id.Should().NotBeEmpty());
    }

    [Fact]
    public void OrderLine_MultipleProducts_ShouldMaintainSeparateLines()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), 
            new Address("Street", "City", "Country", "12345"), "USD");
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        // Act
        order.AddItem(product1Id, "Product 1", CreateUnitPrice(10m), 2);
        order.AddItem(product2Id, "Product 2", CreateUnitPrice(25m), 1);

        // Assert
        order.OrderLines.Should().HaveCount(2);
        order.OrderLines.Should().Contain(l => l.ProductId == product1Id);
        order.OrderLines.Should().Contain(l => l.ProductId == product2Id);
    }

    #endregion
}
