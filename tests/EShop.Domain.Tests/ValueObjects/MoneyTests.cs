using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for Money Value Object
/// Tests immutability, business rules, and equality
/// </summary>
public class MoneyTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_WithZeroAmount_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(0, "USD");

        // Assert
        money.Amount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Arrange & Act
        var act = () => new Money(-10, "USD");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCurrency_ShouldThrowDomainException(string? currency)
    {
        // Arrange & Act
        var act = () => new Money(100, currency!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Currency*required*");
    }

    [Fact]
    public void Constructor_ShouldUppercaseCurrency()
    {
        // Arrange & Act
        var money = new Money(100, "usd");

        // Assert
        money.Currency.Should().Be("USD");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Zero_ShouldCreateMoneyWithZeroAmount()
    {
        // Arrange & Act
        var money = Money.Zero("KWD");

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("KWD");
    }

    [Fact]
    public void FromKWD_ShouldCreateMoneyWithKWDCurrency()
    {
        // Arrange & Act
        var money = Money.FromKWD(50.25m);

        // Assert
        money.Amount.Should().Be(50.25m);
        money.Currency.Should().Be("KWD");
    }

    [Fact]
    public void FromUSD_ShouldCreateMoneyWithUSDCurrency()
    {
        // Arrange & Act
        var money = Money.FromUSD(75.00m);

        // Assert
        money.Amount.Should().Be(75.00m);
        money.Currency.Should().Be("USD");
    }

    #endregion

    #region Add Operation Tests

    [Fact]
    public void Add_WithSameCurrency_ShouldReturnNewMoneyWithSum()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
        // Verify immutability
        money1.Amount.Should().Be(100);
        money2.Amount.Should().Be(50);
    }

    [Fact]
    public void Add_WithDifferentCurrency_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act
        var act = () => money1.Add(money2);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*different currencies*");
    }

    #endregion

    #region Subtract Operation Tests

    [Fact]
    public void Subtract_WithSameCurrency_ShouldReturnNewMoneyWithDifference()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(30, "USD");

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Subtract_WouldResultInNegative_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = new Money(30, "USD");
        var money2 = new Money(100, "USD");

        // Act
        var act = () => money1.Subtract(money2);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void Subtract_WithDifferentCurrency_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(30, "EUR");

        // Act
        var act = () => money1.Subtract(money2);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*different currencies*");
    }

    #endregion

    #region Multiply Operation Tests

    [Fact]
    public void Multiply_WithPositiveFactor_ShouldReturnNewMoneyWithProduct()
    {
        // Arrange
        var money = new Money(25, "USD");

        // Act
        var result = money.Multiply(4);

        // Assert
        result.Amount.Should().Be(100);
        result.Currency.Should().Be("USD");
        // Verify immutability
        money.Amount.Should().Be(25);
    }

    [Fact]
    public void Multiply_WithZeroFactor_ShouldReturnZeroMoney()
    {
        // Arrange
        var money = new Money(100, "USD");

        // Act
        var result = money.Multiply(0);

        // Assert
        result.Amount.Should().Be(0);
    }

    [Fact]
    public void Multiply_WithDecimalFactor_ShouldReturnCorrectAmount()
    {
        // Arrange
        var money = new Money(100, "USD");

        // Act
        var result = money.Multiply(0.15m); // 15% tax

        // Assert
        result.Amount.Should().Be(15);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentAmount_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(200, "USD");

        // Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void Equals_WithDifferentCurrency_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "EUR");

        // Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var money = new Money(1234.56m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Contain("1,234.56");
        result.Should().Contain("USD");
    }

    #endregion
}
