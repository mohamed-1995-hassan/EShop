using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.SupplierAggregate;

/// <summary>
/// Supplier/Purchasing Aggregate - B2B procurement
/// 
/// BUSINESS RULES:
/// - Supplier onboarding and management
/// - Purchase order creation and tracking
/// - Lead time and MOQ management
/// - Supplier performance scoring
/// - Reorder point automation
/// </summary>
public class Supplier : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ContactName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public Address? Address { get; private set; }
    public string? Website { get; private set; }
    public string? TaxId { get; private set; }
    public SupplierStatus Status { get; private set; }
    public SupplierTier Tier { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int DefaultLeadTimeDays { get; private set; }
    public decimal MinimumOrderValue { get; private set; }
    public PaymentTerms PaymentTerms { get; private set; }
    public decimal PerformanceScore { get; private set; }
    public int TotalOrders { get; private set; }
    public int OnTimeDeliveries { get; private set; }
    public int QualityIssues { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastOrderAt { get; private set; }

    private readonly List<SupplierProduct> _products = new();
    public IReadOnlyCollection<SupplierProduct> Products => _products.AsReadOnly();

    private readonly List<SupplierContact> _contacts = new();
    public IReadOnlyCollection<SupplierContact> Contacts => _contacts.AsReadOnly();

    public decimal OnTimeDeliveryRate => TotalOrders > 0 ? (decimal)OnTimeDeliveries / TotalOrders * 100 : 100;
    public decimal QualityRate => TotalOrders > 0 ? 100 - ((decimal)QualityIssues / TotalOrders * 100) : 100;

    private Supplier() { }

    public static Supplier Create(
        string code,
        string name,
        string contactName,
        string email,
        string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Supplier code is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Supplier name is required");

        return new Supplier
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            Name = name,
            ContactName = contactName,
            Email = email,
            Status = SupplierStatus.Pending,
            Tier = SupplierTier.Standard,
            Currency = currency,
            DefaultLeadTimeDays = 14,
            MinimumOrderValue = 0,
            PaymentTerms = PaymentTerms.Net30,
            PerformanceScore = 100,
            TotalOrders = 0,
            OnTimeDeliveries = 0,
            QualityIssues = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Basic Info

    public void UpdateDetails(
        string name,
        string contactName,
        string email,
        string? phone,
        string? website)
    {
        Name = name;
        ContactName = contactName;
        Email = email;
        Phone = phone;
        Website = website;
    }

    public void SetAddress(Address address) => Address = address;
    public void SetTaxId(string taxId) => TaxId = taxId;

    #endregion

    #region Products

    public SupplierProduct AddProduct(
        Guid productId,
        string sku,
        string name,
        Money unitCost,
        int minimumOrderQty = 1,
        int? leadTimeDays = null)
    {
        if (_products.Any(p => p.ProductId == productId))
            throw new DomainException("Product already added to supplier");

        var product = new SupplierProduct(
            Guid.NewGuid(),
            productId,
            sku,
            name,
            unitCost,
            minimumOrderQty,
            leadTimeDays ?? DefaultLeadTimeDays);

        _products.Add(product);
        return product;
    }

    public void UpdateProductCost(Guid productId, Money newCost)
    {
        var product = _products.FirstOrDefault(p => p.ProductId == productId)
            ?? throw new DomainException("Product not found");

        product.UpdateCost(newCost);
    }

    public void RemoveProduct(Guid productId)
    {
        var product = _products.FirstOrDefault(p => p.ProductId == productId);
        if (product != null)
            _products.Remove(product);
    }

    public void SetProductAsPreferred(Guid productId)
    {
        var product = _products.FirstOrDefault(p => p.ProductId == productId)
            ?? throw new DomainException("Product not found");

        product.SetPreferred(true);
    }

    #endregion

    #region Contacts

    public SupplierContact AddContact(string name, string email, string? phone, string? role)
    {
        var contact = new SupplierContact(Guid.NewGuid(), name, email, phone, role);
        _contacts.Add(contact);
        return contact;
    }

    public void RemoveContact(Guid contactId)
    {
        var contact = _contacts.FirstOrDefault(c => c.Id == contactId);
        if (contact != null)
            _contacts.Remove(contact);
    }

    public void SetPrimaryContact(Guid contactId)
    {
        foreach (var contact in _contacts)
            contact.SetPrimary(contact.Id == contactId);
    }

    #endregion

    #region Settings

    public void SetOrderingSettings(
        int defaultLeadTimeDays,
        decimal minimumOrderValue,
        PaymentTerms paymentTerms)
    {
        if (defaultLeadTimeDays < 0)
            throw new DomainException("Lead time cannot be negative");

        DefaultLeadTimeDays = defaultLeadTimeDays;
        MinimumOrderValue = minimumOrderValue;
        PaymentTerms = paymentTerms;
    }

    public void SetTier(SupplierTier tier) => Tier = tier;

    #endregion

    #region Status

    public void Approve()
    {
        if (Status != SupplierStatus.Pending)
            throw new DomainException("Only pending suppliers can be approved");

        Status = SupplierStatus.Active;
    }

    public void Suspend(string reason)
    {
        Status = SupplierStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status != SupplierStatus.Suspended)
            throw new DomainException("Only suspended suppliers can be reactivated");

        Status = SupplierStatus.Active;
    }

    public void Deactivate() => Status = SupplierStatus.Inactive;

    #endregion

    #region Performance

    public void RecordDelivery(bool onTime, bool hasQualityIssue)
    {
        TotalOrders++;
        if (onTime) OnTimeDeliveries++;
        if (hasQualityIssue) QualityIssues++;

        UpdatePerformanceScore();
        LastOrderAt = DateTime.UtcNow;
    }

    private void UpdatePerformanceScore()
    {
        // Weighted score: 60% on-time, 40% quality
        PerformanceScore = (OnTimeDeliveryRate * 0.6m) + (QualityRate * 0.4m);

        // Update tier based on performance
        Tier = PerformanceScore switch
        {
            >= 95 when TotalOrders >= 50 => SupplierTier.Strategic,
            >= 90 when TotalOrders >= 20 => SupplierTier.Preferred,
            >= 80 => SupplierTier.Standard,
            _ => SupplierTier.Probationary
        };
    }

    #endregion
}

public class SupplierProduct : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public Money UnitCost { get; private set; }
    public int MinimumOrderQty { get; private set; }
    public int LeadTimeDays { get; private set; }
    public bool IsPreferred { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastOrderedAt { get; private set; }

    internal SupplierProduct(
        Guid id,
        Guid productId,
        string sku,
        string name,
        Money unitCost,
        int minimumOrderQty,
        int leadTimeDays)
    {
        Id = id;
        ProductId = productId;
        Sku = sku;
        Name = name;
        UnitCost = unitCost;
        MinimumOrderQty = minimumOrderQty;
        LeadTimeDays = leadTimeDays;
        IsPreferred = false;
        IsActive = true;
    }

    private SupplierProduct() { }

    internal void UpdateCost(Money newCost) => UnitCost = newCost;
    internal void SetPreferred(bool preferred) => IsPreferred = preferred;
    internal void RecordOrder() => LastOrderedAt = DateTime.UtcNow;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class SupplierContact : Entity<Guid>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Role { get; private set; }
    public bool IsPrimary { get; private set; }

    internal SupplierContact(Guid id, string name, string email, string? phone, string? role)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        Role = role;
        IsPrimary = false;
    }

    private SupplierContact() { }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}

/// <summary>
/// Purchase Order - Ordering from suppliers
/// </summary>
public class PurchaseOrder : AggregateRoot<Guid>
{
    public string PoNumber { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = string.Empty;
    public Guid? WarehouseId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public Money Subtotal { get; private set; } = null!;
    public Money Tax { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money Total { get; private set; } = null!;
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    public string? Notes { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = new();
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    private readonly List<PurchaseOrderReceipt> _receipts = new();
    public IReadOnlyCollection<PurchaseOrderReceipt> Receipts => _receipts.AsReadOnly();

    public int TotalQuantity => _lines.Sum(l => l.Quantity);
    public int ReceivedQuantity => _receipts.Sum(r => r.QuantityReceived);
    public bool IsFullyReceived => ReceivedQuantity >= TotalQuantity;

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        Guid supplierId,
        string supplierName,
        Guid createdBy,
        string currency,
        Guid? warehouseId = null)
    {
        return new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = GeneratePoNumber(),
            SupplierId = supplierId,
            SupplierName = supplierName,
            WarehouseId = warehouseId,
            Status = PurchaseOrderStatus.Draft,
            Subtotal = Money.Zero(currency),
            Tax = Money.Zero(currency),
            ShippingCost = Money.Zero(currency),
            Total = Money.Zero(currency),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Lines

    public PurchaseOrderLine AddLine(
        Guid productId,
        string productName,
        string sku,
        int quantity,
        Money unitCost)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("Cannot modify sent purchase order");

        var line = new PurchaseOrderLine(
            Guid.NewGuid(),
            productId,
            productName,
            sku,
            quantity,
            unitCost);

        _lines.Add(line);
        RecalculateTotals();
        return line;
    }

    public void UpdateLineQuantity(Guid lineId, int newQuantity)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("Cannot modify sent purchase order");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException("Line not found");

        line.UpdateQuantity(newQuantity);
        RecalculateTotals();
    }

    public void RemoveLine(Guid lineId)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("Cannot modify sent purchase order");

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line != null)
        {
            _lines.Remove(line);
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        var currency = Subtotal.Currency;
        Subtotal = new Money(_lines.Sum(l => l.LineTotal.Amount), currency);
        Total = Subtotal.Add(Tax).Add(ShippingCost);
    }

    #endregion

    #region Lifecycle

    public void SetShippingAndTax(Money shipping, Money tax)
    {
        ShippingCost = shipping;
        Tax = tax;
        RecalculateTotals();
    }

    public void SetExpectedDeliveryDate(DateTime date)
    {
        ExpectedDeliveryDate = date;
    }

    public void Submit()
    {
        if (!_lines.Any())
            throw new DomainException("Purchase order must have at least one line");

        Status = PurchaseOrderStatus.PendingApproval;
    }

    public void Approve(Guid approvedBy)
    {
        if (Status != PurchaseOrderStatus.PendingApproval)
            throw new DomainException("Only pending orders can be approved");

        Status = PurchaseOrderStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != PurchaseOrderStatus.PendingApproval)
            throw new DomainException("Only pending orders can be rejected");

        Status = PurchaseOrderStatus.Rejected;
    }

    public void SendToSupplier()
    {
        if (Status != PurchaseOrderStatus.Approved)
            throw new DomainException("Only approved orders can be sent");

        Status = PurchaseOrderStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsAcknowledged()
    {
        if (Status != PurchaseOrderStatus.Sent)
            throw new DomainException("Only sent orders can be acknowledged");

        Status = PurchaseOrderStatus.Acknowledged;
    }

    #endregion

    #region Receiving

    public PurchaseOrderReceipt ReceiveGoods(
        Guid receivedBy,
        Dictionary<Guid, int> quantitiesReceived,
        string? notes = null)
    {
        if (Status != PurchaseOrderStatus.Sent && 
            Status != PurchaseOrderStatus.Acknowledged &&
            Status != PurchaseOrderStatus.PartiallyReceived)
            throw new DomainException("Order must be sent before receiving goods");

        var receipt = new PurchaseOrderReceipt(
            Guid.NewGuid(),
            receivedBy,
            quantitiesReceived.Sum(q => q.Value),
            notes);

        _receipts.Add(receipt);

        foreach (var (lineId, qty) in quantitiesReceived)
        {
            var line = _lines.FirstOrDefault(l => l.Id == lineId);
            line?.ReceiveQuantity(qty);
        }

        Status = IsFullyReceived 
            ? PurchaseOrderStatus.Received 
            : PurchaseOrderStatus.PartiallyReceived;

        if (IsFullyReceived)
            ActualDeliveryDate = DateTime.UtcNow;

        return receipt;
    }

    public void Complete()
    {
        if (Status != PurchaseOrderStatus.Received)
            throw new DomainException("Only fully received orders can be completed");

        Status = PurchaseOrderStatus.Completed;
    }

    public void Cancel(string reason)
    {
        if (Status == PurchaseOrderStatus.Completed || Status == PurchaseOrderStatus.Received)
            throw new DomainException("Cannot cancel completed order");

        Status = PurchaseOrderStatus.Cancelled;
    }

    #endregion

    private static string GeneratePoNumber()
    {
        return $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

public class PurchaseOrderLine : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public string Sku { get; private set; }
    public int Quantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public Money UnitCost { get; private set; }
    public Money LineTotal { get; private set; }

    internal PurchaseOrderLine(
        Guid id,
        Guid productId,
        string productName,
        string sku,
        int quantity,
        Money unitCost)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        Quantity = quantity;
        ReceivedQuantity = 0;
        UnitCost = unitCost;
        LineTotal = unitCost.Multiply(quantity);
    }

    private PurchaseOrderLine() { }

    internal void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
        LineTotal = UnitCost.Multiply(quantity);
    }

    internal void ReceiveQuantity(int qty)
    {
        ReceivedQuantity += qty;
    }
}

public class PurchaseOrderReceipt : Entity<Guid>
{
    public Guid ReceivedBy { get; private set; }
    public int QuantityReceived { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    internal PurchaseOrderReceipt(Guid id, Guid receivedBy, int quantityReceived, string? notes)
    {
        Id = id;
        ReceivedBy = receivedBy;
        QuantityReceived = quantityReceived;
        Notes = notes;
        ReceivedAt = DateTime.UtcNow;
    }

    private PurchaseOrderReceipt() { }
}

public enum SupplierStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Inactive = 4
}

public enum SupplierTier
{
    Probationary = 1,
    Standard = 2,
    Preferred = 3,
    Strategic = 4
}

public enum PaymentTerms
{
    Prepaid = 1,
    Net15 = 2,
    Net30 = 3,
    Net45 = 4,
    Net60 = 5,
    COD = 6
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Sent = 5,
    Acknowledged = 6,
    PartiallyReceived = 7,
    Received = 8,
    Completed = 9,
    Cancelled = 10
}
