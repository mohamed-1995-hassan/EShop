using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for Address Value Object
/// Tests validation, immutability, and equality
/// </summary>
public class AddressTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateAddress()
    {
        // Arrange & Act
        var address = new Address("123 Main St", "New York", "USA", "10001");

        // Assert
        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("New York");
        address.Country.Should().Be("USA");
        address.PostalCode.Should().Be("10001");
    }

    [Fact]
    public void Constructor_WithEmptyPostalCode_ShouldAllowCreation()
    {
        // Arrange & Act
        var address = new Address("123 Main St", "New York", "USA", "");

        // Assert
        address.PostalCode.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullPostalCode_ShouldConvertToEmpty()
    {
        // Arrange & Act
        var address = new Address("123 Main St", "New York", "USA", null!);

        // Assert
        address.PostalCode.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidStreet_ShouldThrowDomainException(string? street)
    {
        // Arrange & Act
        var act = () => new Address(street!, "New York", "USA", "10001");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Street*required*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCity_ShouldThrowDomainException(string? city)
    {
        // Arrange & Act
        var act = () => new Address("123 Main St", city!, "USA", "10001");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*City*required*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCountry_ShouldThrowDomainException(string? country)
    {
        // Arrange & Act
        var act = () => new Address("123 Main St", "New York", country!, "10001");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Country*required*");
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA", "10001");
        var address2 = new Address("123 Main St", "New York", "USA", "10001");

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentStreet_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA", "10001");
        var address2 = new Address("456 Oak Ave", "New York", "USA", "10001");

        // Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentCity_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA", "10001");
        var address2 = new Address("123 Main St", "Los Angeles", "USA", "90001");

        // Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA", "10001");
        var address2 = new Address("123 Main St", "New York", "USA", "10001");

        // Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedAddress()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "USA", "10001");

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Contain("123 Main St");
        result.Should().Contain("New York");
        result.Should().Contain("USA");
        result.Should().Contain("10001");
    }

    #endregion
}
