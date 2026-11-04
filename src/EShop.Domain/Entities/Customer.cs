using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Entities;

/// <summary>
/// Entity - identified by CustomerId, not by name/email
/// </summary>
public class Customer : Entity<Guid>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public Address? ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private Customer() { } // For EF Core
    
    public Customer(Guid id, string name, string email)
    {
        if (id == Guid.Empty)
            throw new DomainException("Customer ID is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Customer email is required");
        
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required");
        
        Name = name;
    }
    
    public void SetShippingAddress(Address address)
    {
        ShippingAddress = address ?? throw new DomainException("Address cannot be null");
    }
}
