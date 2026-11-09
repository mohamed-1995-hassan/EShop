using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.ShippingAggregate;

/// <summary>
/// Carrier Aggregate - Shipping provider configuration
/// 
/// BUSINESS RULES:
/// - Each carrier has service levels with rates
/// - Zone-based pricing supported
/// - Weight/dimension limits per service
/// - Active/Inactive status controls availability
/// </summary>
public class Carrier : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? TrackingUrlTemplate { get; private set; }
    public string? ApiEndpoint { get; private set; }
    public bool IsActive { get; private set; }
    public bool SupportsInternational { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<CarrierService> _services = new();
    public IReadOnlyCollection<CarrierService> Services => _services.AsReadOnly();

    private readonly List<ShippingZone> _zones = new();
    public IReadOnlyCollection<ShippingZone> Zones => _zones.AsReadOnly();

    private Carrier() { }

    public static Carrier Create(
        string code,
        string name,
        string? trackingUrlTemplate = null,
        bool supportsInternational = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Carrier code is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Carrier name is required");

        return new Carrier
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            Name = name,
            TrackingUrlTemplate = trackingUrlTemplate,
            SupportsInternational = supportsInternational,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public CarrierService AddService(
        string code,
        string name,
        ShipmentType shipmentType,
        int estimatedDaysMin,
        int estimatedDaysMax,
        decimal baseRate,
        string currency)
    {
        if (_services.Any(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Service '{code}' already exists");

        var service = new CarrierService(
            Guid.NewGuid(),
            code,
            name,
            shipmentType,
            estimatedDaysMin,
            estimatedDaysMax,
            new Money(baseRate, currency));

        _services.Add(service);
        return service;
    }

    public ShippingZone AddZone(string name, IEnumerable<string> countryCodes)
    {
        var zone = new ShippingZone(Guid.NewGuid(), name, countryCodes.ToList());
        _zones.Add(zone);
        return zone;
    }

    public void SetApiEndpoint(string endpoint) => ApiEndpoint = endpoint;
    public void SetLogoUrl(string logoUrl) => LogoUrl = logoUrl;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public string GetTrackingUrl(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(TrackingUrlTemplate))
            return string.Empty;

        return TrackingUrlTemplate.Replace("{tracking}", trackingNumber);
    }

    public CarrierService? GetService(ShipmentType type)
    {
        return _services.FirstOrDefault(s => s.ShipmentType == type && s.IsActive);
    }

    public Money CalculateShippingCost(
        ShipmentType shipmentType,
        decimal weight,
        string destinationCountry)
    {
        var service = GetService(shipmentType)
            ?? throw new DomainException($"Service type {shipmentType} not available");

        if (!service.CanShip(weight))
            throw new DomainException($"Weight {weight}kg exceeds limit for {service.Name}");

        var zone = _zones.FirstOrDefault(z => z.ContainsCountry(destinationCountry));
        var zoneMultiplier = zone?.RateMultiplier ?? 1.0m;

        var weightCharge = service.CalculateWeightCharge(weight);
        var totalRate = service.BaseRate.Add(weightCharge).Multiply(zoneMultiplier);

        return totalRate;
    }
}

public class CarrierService : Entity<Guid>
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public ShipmentType ShipmentType { get; private set; }
    public int EstimatedDaysMin { get; private set; }
    public int EstimatedDaysMax { get; private set; }
    public Money BaseRate { get; private set; }
    public decimal? MaxWeightKg { get; private set; }
    public decimal WeightRatePerKg { get; private set; }
    public bool IsActive { get; private set; }

    internal CarrierService(
        Guid id,
        string code,
        string name,
        ShipmentType shipmentType,
        int estimatedDaysMin,
        int estimatedDaysMax,
        Money baseRate)
    {
        Id = id;
        Code = code.ToUpperInvariant();
        Name = name;
        ShipmentType = shipmentType;
        EstimatedDaysMin = estimatedDaysMin;
        EstimatedDaysMax = estimatedDaysMax;
        BaseRate = baseRate;
        WeightRatePerKg = 0.5m; // Default
        IsActive = true;
    }

    private CarrierService() { } // EF Core

    public void SetWeightLimits(decimal maxWeightKg, decimal ratePerKg)
    {
        MaxWeightKg = maxWeightKg;
        WeightRatePerKg = ratePerKg;
    }

    public bool CanShip(decimal weight)
    {
        return !MaxWeightKg.HasValue || weight <= MaxWeightKg.Value;
    }

    public Money CalculateWeightCharge(decimal weight)
    {
        return new Money(weight * WeightRatePerKg, BaseRate.Currency);
    }

    public string GetEstimatedDelivery()
    {
        return EstimatedDaysMin == EstimatedDaysMax
            ? $"{EstimatedDaysMin} days"
            : $"{EstimatedDaysMin}-{EstimatedDaysMax} days";
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class ShippingZone : Entity<Guid>
{
    public string Name { get; private set; }
    public decimal RateMultiplier { get; private set; }

    private readonly List<string> _countryCodes = new();
    public IReadOnlyCollection<string> CountryCodes => _countryCodes.AsReadOnly();

    internal ShippingZone(Guid id, string name, List<string> countryCodes)
    {
        Id = id;
        Name = name;
        _countryCodes = countryCodes;
        RateMultiplier = 1.0m;
    }

    private ShippingZone() { } // EF Core

    public void SetRateMultiplier(decimal multiplier)
    {
        if (multiplier <= 0)
            throw new DomainException("Rate multiplier must be greater than zero");
        RateMultiplier = multiplier;
    }

    public void AddCountry(string countryCode)
    {
        if (!_countryCodes.Contains(countryCode.ToUpperInvariant()))
            _countryCodes.Add(countryCode.ToUpperInvariant());
    }

    public bool ContainsCountry(string countryCode)
    {
        return _countryCodes.Contains(countryCode.ToUpperInvariant());
    }
}
