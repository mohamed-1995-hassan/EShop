using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.ValueObjects;

/// <summary>
/// Value Object representing a shipping address
/// </summary>
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string Country { get; }
    public string PostalCode { get; }
    
    private Address() { } // For EF Core
    
    public Address(string street, string city, string country, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("Street is required");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City is required");
        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Country is required");
        
        Street = street;
        City = city;
        Country = country;
        PostalCode = postalCode ?? string.Empty;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
    
    public override string ToString() => $"{Street}, {City}, {Country} {PostalCode}";
}
