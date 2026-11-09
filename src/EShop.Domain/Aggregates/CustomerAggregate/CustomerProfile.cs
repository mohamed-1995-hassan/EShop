using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.CustomerAggregate;

/// <summary>
/// Customer Profile Aggregate - Full customer with loyalty, wallet, preferences
/// 
/// BUSINESS RULES:
/// - Loyalty points expire after 12 months
/// - Tier upgrades based on annual spend
/// - Wallet balance cannot go negative
/// - Referral bonus only once per referred customer
/// </summary>
public class CustomerProfile : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public CustomerTier Tier { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public decimal TotalLifetimeSpend { get; private set; }
    public decimal CurrentYearSpend { get; private set; }
    public int TotalOrders { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastOrderAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool EmailVerified { get; private set; }
    public Guid? ReferredByCustomerId { get; private set; }

    private readonly List<CustomerAddress> _addresses = new();
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

    private readonly List<PaymentMethod> _paymentMethods = new();
    public IReadOnlyCollection<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    private readonly List<CustomerWalletTransaction> _walletTransactions = new();
    public IReadOnlyCollection<CustomerWalletTransaction> WalletTransactions => _walletTransactions.AsReadOnly();

    private readonly List<LoyaltyPointTransaction> _loyaltyTransactions = new();
    public IReadOnlyCollection<LoyaltyPointTransaction> LoyaltyTransactions => _loyaltyTransactions.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}";
    public decimal WalletBalance => _walletTransactions.Sum(t => t.Amount);

    private CustomerProfile() { }

    public static CustomerProfile Create(
        string email,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string? phone = null,
        Guid? referredByCustomerId = null)
    {
        ValidateEmail(email);
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");
        if (dateOfBirth > DateTime.UtcNow.AddYears(-13))
            throw new DomainException("Customer must be at least 13 years old");

        var customer = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Tier = CustomerTier.Bronze,
            LoyaltyPoints = 0,
            TotalLifetimeSpend = 0,
            CurrentYearSpend = 0,
            TotalOrders = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailVerified = false,
            ReferredByCustomerId = referredByCustomerId
        };

        // Welcome bonus
        customer.AddLoyaltyPoints(100, "Welcome bonus", LoyaltyTransactionType.Bonus);

        return customer;
    }

    #region Profile Management

    public void UpdateProfile(string firstName, string lastName, string? phone, DateTime dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");

        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        DateOfBirth = dateOfBirth;
    }

    public void VerifyEmail()
    {
        if (EmailVerified)
            throw new DomainException("Email already verified");

        EmailVerified = true;
        AddLoyaltyPoints(50, "Email verification bonus", LoyaltyTransactionType.Bonus);
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;

    #endregion

    #region Address Management

    public CustomerAddress AddAddress(
        string label,
        Address address,
        bool isDefault = false)
    {
        if (_addresses.Count >= 10)
            throw new DomainException("Maximum 10 addresses allowed");

        if (isDefault)
        {
            foreach (var addr in _addresses)
                addr.SetAsNonDefault();
        }

        var customerAddress = new CustomerAddress(
            Guid.NewGuid(), 
            label, 
            address, 
            isDefault || !_addresses.Any());

        _addresses.Add(customerAddress);
        return customerAddress;
    }

    public void RemoveAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new DomainException("Address not found");

        _addresses.Remove(address);

        // If removed default, set first as default
        if (address.IsDefault && _addresses.Any())
            _addresses.First().SetAsDefault();
    }

    public void SetDefaultAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new DomainException("Address not found");

        foreach (var addr in _addresses)
            addr.SetAsNonDefault();

        address.SetAsDefault();
    }

    public CustomerAddress? GetDefaultAddress() => _addresses.FirstOrDefault(a => a.IsDefault);

    #endregion

    #region Payment Methods

    public PaymentMethod AddPaymentMethod(
        PaymentMethodType type,
        string lastFourDigits,
        string? cardBrand = null,
        DateTime? expiryDate = null,
        bool isDefault = false)
    {
        if (_paymentMethods.Count >= 5)
            throw new DomainException("Maximum 5 payment methods allowed");

        if (isDefault)
        {
            foreach (var pm in _paymentMethods)
                pm.SetAsNonDefault();
        }

        var paymentMethod = new PaymentMethod(
            Guid.NewGuid(),
            type,
            lastFourDigits,
            cardBrand,
            expiryDate,
            isDefault || !_paymentMethods.Any());

        _paymentMethods.Add(paymentMethod);
        return paymentMethod;
    }

    public void RemovePaymentMethod(Guid paymentMethodId)
    {
        var pm = _paymentMethods.FirstOrDefault(p => p.Id == paymentMethodId)
            ?? throw new DomainException("Payment method not found");

        _paymentMethods.Remove(pm);

        if (pm.IsDefault && _paymentMethods.Any())
            _paymentMethods.First().SetAsDefault();
    }

    #endregion

    #region Wallet Operations

    public void CreditWallet(decimal amount, string description, WalletTransactionType type, Guid? referenceId = null)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero");

        _walletTransactions.Add(new CustomerWalletTransaction(
            Guid.NewGuid(),
            amount,
            description,
            type,
            referenceId));
    }

    public void DebitWallet(decimal amount, string description, WalletTransactionType type, Guid? referenceId = null)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero");

        if (WalletBalance < amount)
            throw new DomainException($"Insufficient wallet balance. Available: {WalletBalance:N2}");

        _walletTransactions.Add(new CustomerWalletTransaction(
            Guid.NewGuid(),
            -amount,
            description,
            type,
            referenceId));
    }

    #endregion

    #region Loyalty Points

    public void AddLoyaltyPoints(int points, string description, LoyaltyTransactionType type, Guid? orderId = null)
    {
        if (points <= 0)
            throw new DomainException("Points must be greater than zero");

        LoyaltyPoints += points;
        _loyaltyTransactions.Add(new LoyaltyPointTransaction(
            Guid.NewGuid(),
            points,
            description,
            type,
            orderId,
            DateTime.UtcNow.AddMonths(12))); // Points expire in 12 months
    }

    public void RedeemLoyaltyPoints(int points, string description, Guid? orderId = null)
    {
        if (points <= 0)
            throw new DomainException("Points must be greater than zero");

        if (LoyaltyPoints < points)
            throw new DomainException($"Insufficient points. Available: {LoyaltyPoints}");

        LoyaltyPoints -= points;
        _loyaltyTransactions.Add(new LoyaltyPointTransaction(
            Guid.NewGuid(),
            -points,
            description,
            LoyaltyTransactionType.Redemption,
            orderId,
            null));
    }

    public int CalculatePointsForPurchase(decimal amount)
    {
        // Base: 1 point per 1 currency unit
        var basePoints = (int)Math.Floor(amount);
        
        // Tier multiplier
        var multiplier = Tier switch
        {
            CustomerTier.Bronze => 1.0m,
            CustomerTier.Silver => 1.25m,
            CustomerTier.Gold => 1.5m,
            CustomerTier.Platinum => 2.0m,
            _ => 1.0m
        };

        return (int)Math.Floor(basePoints * multiplier);
    }

    public decimal GetPointsValue(int points)
    {
        // 100 points = 1 currency unit
        return points / 100m;
    }

    #endregion

    #region Order & Spend Tracking

    public void RecordOrder(decimal orderTotal)
    {
        TotalOrders++;
        TotalLifetimeSpend += orderTotal;
        CurrentYearSpend += orderTotal;
        LastOrderAt = DateTime.UtcNow;

        // Add loyalty points
        var points = CalculatePointsForPurchase(orderTotal);
        AddLoyaltyPoints(points, $"Order purchase ({orderTotal:N2})", LoyaltyTransactionType.Purchase);

        // Check tier upgrade
        UpdateTier();
    }

    public void ResetYearlySpend()
    {
        CurrentYearSpend = 0;
        UpdateTier();
    }

    private void UpdateTier()
    {
        var newTier = CurrentYearSpend switch
        {
            >= 10000 => CustomerTier.Platinum,
            >= 5000 => CustomerTier.Gold,
            >= 1000 => CustomerTier.Silver,
            _ => CustomerTier.Bronze
        };

        if (newTier > Tier)
        {
            var oldTier = Tier;
            Tier = newTier;
            
            // Tier upgrade bonus
            var bonusPoints = (newTier - oldTier) * 500;
            AddLoyaltyPoints(bonusPoints, $"Tier upgrade to {newTier}", LoyaltyTransactionType.Bonus);
        }
        else if (newTier < Tier)
        {
            Tier = newTier; // Downgrade (no bonus)
        }
    }

    #endregion

    #region Referral

    public void ProcessReferralBonus(decimal referredOrderTotal)
    {
        // 10% of first order as wallet credit
        var bonus = Math.Min(referredOrderTotal * 0.1m, 50m); // Cap at 50
        CreditWallet(bonus, "Referral bonus", WalletTransactionType.ReferralBonus);
        AddLoyaltyPoints(200, "Referral bonus", LoyaltyTransactionType.Bonus);
    }

    #endregion

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required");
        if (!email.Contains('@') || !email.Contains('.'))
            throw new DomainException("Invalid email format");
    }
}

public enum CustomerTier
{
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4
}
