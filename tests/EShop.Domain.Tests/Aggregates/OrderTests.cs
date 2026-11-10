using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Events;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Tests.Aggregates;

/// <summary>
/// Unit tests for Order Aggregate
/// Tests business rules, state transitions, and domain events
/// </summary>
public class OrderTests
{
    private static Address CreateValidAddress() =>
        new Address("123 Main St", "New York", "USA", "10001");

    private static Money CreateUnitPrice(decimal amount = 10m) =>
        Money.FromUSD(amount);

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var address = CreateValidAddress();

        // Act
        var order = Order.Create(customerId, address);

        // Assert
        order.Id.Should().NotBeEmpty();
        order.CustomerId.Should().Be(customerId);
        order.ShippingAddress.Should().Be(address);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Amount.Should().Be(0);
        order.OrderLines.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ShouldThrowDomainException()
    {
        // Arrange
        var address = CreateValidAddress();

        // Act
        var act = () => Order.Create(Guid.Empty, address);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Customer ID*required*");
    }

    [Fact]
    public void Create_WithNullAddress_ShouldThrowArgumentNullException()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var act = () => Order.Create(customerId, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithCurrency_ShouldUseCurrencyForTotalAmount()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var address = CreateValidAddress();

        // Act
        var order = Order.Create(customerId, address, "KWD");

        // Assert
        order.TotalAmount.Currency.Should().Be("KWD");
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_ToNewOrder_ShouldAddOrderLine()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        var productId = Guid.NewGuid();
        var unitPrice = CreateUnitPrice(25m);

        // Act
        order.AddItem(productId, "Test Product", unitPrice, 2);

        // Assert
        order.OrderLines.Should().HaveCount(1);
        var line = order.OrderLines.First();
        line.ProductId.Should().Be(productId);
        line.ProductName.Should().Be("Test Product");
        line.Quantity.Should().Be(2);
        line.UnitPrice.Should().Be(unitPrice);
    }

    [Fact]
    public void AddItem_ShouldRecalculateTotalAmount()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        var unitPrice = CreateUnitPrice(25m);

        // Act
        order.AddItem(Guid.NewGuid(), "Product 1", unitPrice, 2);
        order.AddItem(Guid.NewGuid(), "Product 2", CreateUnitPrice(10m), 3);

        // Assert
        order.TotalAmount.Amount.Should().Be(80m); // (25*2) + (10*3) = 50 + 30
    }

    [Fact]
    public void AddItem_SameProduct_ShouldConsolidateQuantity()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        var productId = Guid.NewGuid();
        var unitPrice = CreateUnitPrice(10m);

        // Act
        order.AddItem(productId, "Product", unitPrice, 2);
        order.AddItem(productId, "Product", unitPrice, 3);

        // Assert
        order.OrderLines.Should().HaveCount(1);
        order.OrderLines.First().Quantity.Should().Be(5);
        order.TotalAmount.Amount.Should().Be(50m);
    }

    [Fact]
    public void AddItem_ShouldRaiseDomainEvent()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        var productId = Guid.NewGuid();

        // Act
        order.AddItem(productId, "Product", CreateUnitPrice(), 1);

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is OrderItemAddedEvent);
        var @event = order.DomainEvents.OfType<OrderItemAddedEvent>().First();
        @event.OrderId.Should().Be(order.Id);
        @event.ProductId.Should().Be(productId);
    }

    [Fact]
    public void AddItem_ToPlacedOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();

        // Act
        var act = () => order.AddItem(Guid.NewGuid(), "Product 2", CreateUnitPrice(), 1);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot add items*Placed*");
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");

        // Act
        var act = () => order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Quantity*greater than zero*");
    }

    [Fact]
    public void AddItem_WithNegativeQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");

        // Act
        var act = () => order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), -1);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Quantity*greater than zero*");
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public void RemoveItem_ExistingProduct_ShouldRemoveFromOrder()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        var productId = Guid.NewGuid();
        order.AddItem(productId, "Product", CreateUnitPrice(20m), 2);

        // Act
        order.RemoveItem(productId);

        // Assert
        order.OrderLines.Should().BeEmpty();
        order.TotalAmount.Amount.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_NonExistingProduct_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");

        // Act
        var act = () => order.RemoveItem(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void RemoveItem_FromPlacedOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        var productId = Guid.NewGuid();
        order.AddItem(productId, "Product", CreateUnitPrice(), 1);
        order.Place();

        // Act
        var act = () => order.RemoveItem(productId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot remove*Placed*");
    }

    #endregion

    #region Place Tests

    [Fact]
    public void Place_ValidOrder_ShouldChangeStatusToPlaced()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(50m), 1);

        // Act
        order.Place();

        // Assert
        order.Status.Should().Be(OrderStatus.Placed);
    }

    [Fact]
    public void Place_ShouldRaiseOrderPlacedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(50m), 2);
        order.ClearDomainEvents(); // Clear AddItem events

        // Act
        order.Place();

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is OrderPlacedEvent);
        var @event = order.DomainEvents.OfType<OrderPlacedEvent>().First();
        @event.OrderId.Should().Be(order.Id);
        @event.CustomerId.Should().Be(customerId);
        @event.TotalAmount.Should().Be(100m);
    }

    [Fact]
    public void Place_EmptyOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");

        // Act
        var act = () => order.Place();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot place*without items*");
    }

    [Fact]
    public void Place_AlreadyPlacedOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();

        // Act
        var act = () => order.Place();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot place*Placed*");
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public void Confirm_PlacedOrder_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_ShouldRaiseOrderConfirmedEvent()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(100m), 1);
        order.Place();
        order.ClearDomainEvents();

        // Act
        order.Confirm();

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void Confirm_PendingOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);

        // Act
        var act = () => order.Confirm();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*must be placed*confirmation*");
    }

    #endregion

    #region Ship Tests

    [Fact]
    public void Ship_PlacedOrder_ShouldChangeStatusToShipped()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();

        // Act
        order.Ship("TRACK123", "FedEx");

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_ConfirmedOrder_ShouldChangeStatusToShipped()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();
        order.Confirm();

        // Act
        order.Ship();

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_ShouldRaiseOrderShippedEvent()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();
        order.ClearDomainEvents();

        // Act
        order.Ship("TRACK123", "FedEx");

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is OrderShippedEvent);
        var @event = order.DomainEvents.OfType<OrderShippedEvent>().First();
        @event.TrackingNumber.Should().Be("TRACK123");
        @event.Carrier.Should().Be("FedEx");
    }

    [Fact]
    public void Ship_PendingOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);

        // Act
        var act = () => order.Ship();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*must be placed or confirmed*");
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_PendingOrder_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);

        // Act
        order.Cancel("Customer changed mind");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_PlacedOrder_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldRaiseOrderCancelledEvent()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(100m), 1);
        order.Place();
        order.ClearDomainEvents();

        // Act
        order.Cancel("Testing cancellation");

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledEvent);
        var @event = order.DomainEvents.OfType<OrderCancelledEvent>().First();
        @event.Reason.Should().Be("Testing cancellation");
        @event.RefundAmount.Should().Be(100m);
    }

    [Fact]
    public void Cancel_ShippedOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();
        order.Ship();

        // Act
        var act = () => order.Cancel();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot cancel*shipped*");
    }

    [Fact]
    public void Cancel_AlreadyCancelledOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Cancel();

        // Act
        var act = () => order.Cancel();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already cancelled*");
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_ShippedOrder_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();
        order.Ship();

        // Act
        order.Complete();

        // Assert
        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void Complete_NonShippedOrder_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");
        order.AddItem(Guid.NewGuid(), "Product", CreateUnitPrice(), 1);
        order.Place();

        // Act
        var act = () => order.Complete();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*must be shipped*");
    }

    #endregion

    #region Order Lifecycle Tests

    [Fact]
    public void FullOrderLifecycle_ShouldTransitionCorrectly()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), CreateValidAddress(), "USD");

        // Assert initial state
        order.Status.Should().Be(OrderStatus.Pending);

        // Add items
        order.AddItem(Guid.NewGuid(), "Product 1", CreateUnitPrice(50m), 2);
        order.AddItem(Guid.NewGuid(), "Product 2", CreateUnitPrice(25m), 1);
        order.TotalAmount.Amount.Should().Be(125m);

        // Place order
        order.Place();
        order.Status.Should().Be(OrderStatus.Placed);

        // Confirm order
        order.Confirm();
        order.Status.Should().Be(OrderStatus.Confirmed);

        // Ship order
        order.Ship("TRACK123");
        order.Status.Should().Be(OrderStatus.Shipped);

        // Complete order
        order.Complete();
        order.Status.Should().Be(OrderStatus.Completed);
    }

    #endregion
}
