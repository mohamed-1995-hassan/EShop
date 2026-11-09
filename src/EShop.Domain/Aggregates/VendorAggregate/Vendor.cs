using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.VendorAggregate;

/// <summary>
/// Vendor/Seller Aggregate - Marketplace multi-seller support
/// 
/// BUSINESS RULES:
/// - Vendors must be approved before selling
/// - Commission rates vary by category/tier
/// - Payout schedules (weekly, bi-weekly, monthly)
/// - Performance metrics tracked
/// - Suspension for policy violations
/// - Product approval workflow
/// </summary>
public class Vendor : AggregateRoot<Guid>
{
    public string VendorCode { get; private set; } = string.Empty;
    public string BusinessName { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string ContactPhone { get; private set; } = string.Empty;
    public Address BusinessAddress { get; private set; } = null!;
    public VendorStatus Status { get; private set; }
    public VendorTier Tier { get; private set; }
    public decimal DefaultCommissionRate { get; private set; }
    public PayoutSchedule PayoutSchedule { get; private set; }
    public int MinimumPayoutAmount { get; private set; }
    public string? TaxId { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? LastActiveAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }

    // Performance Metrics
    public int TotalOrders { get; private set; }
    public int TotalProductsSold { get; private set; }
    public Money TotalRevenue { get; private set; } = null!;
    public decimal AverageRating { get; private set; }
    public int TotalReviews { get; private set; }
    public decimal OrderFulfillmentRate { get; private set; }
    public decimal OnTimeShippingRate { get; private set; }
    public decimal ReturnRate { get; private set; }

    private readonly List<VendorCategory> _categories = new();
    public IReadOnlyCollection<VendorCategory> Categories => _categories.AsReadOnly();

    private readonly List<VendorDocument> _documents = new();
    public IReadOnlyCollection<VendorDocument> Documents => _documents.AsReadOnly();

    private readonly List<VendorPayout> _payouts = new();
    public IReadOnlyCollection<VendorPayout> Payouts => _payouts.AsReadOnly();

    private readonly List<VendorNote> _notes = new();
    public IReadOnlyCollection<VendorNote> Notes => _notes.AsReadOnly();

    public bool IsActive => Status == VendorStatus.Active;
    public bool CanSell => IsActive && _categories.Any(c => c.IsApproved);

    private Vendor() { }

    public static Vendor Register(
        string businessName,
        string legalName,
        string contactEmail,
        string contactPhone,
        Address businessAddress,
        string? taxId = null)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new DomainException("Business name is required");
        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new DomainException("Contact email is required");

        return new Vendor
        {
            Id = Guid.NewGuid(),
            VendorCode = GenerateVendorCode(),
            BusinessName = businessName,
            LegalName = legalName,
            ContactEmail = contactEmail.ToLowerInvariant(),
            ContactPhone = contactPhone,
            BusinessAddress = businessAddress,
            TaxId = taxId,
            Status = VendorStatus.Pending,
            Tier = VendorTier.Standard,
            DefaultCommissionRate = 15, // 15% default
            PayoutSchedule = PayoutSchedule.BiWeekly,
            MinimumPayoutAmount = 50,
            TotalOrders = 0,
            TotalProductsSold = 0,
            TotalRevenue = Money.Zero("KWD"),
            AverageRating = 0,
            TotalReviews = 0,
            OrderFulfillmentRate = 100,
            OnTimeShippingRate = 100,
            ReturnRate = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Approval Workflow

    public void SubmitForApproval()
    {
        if (Status != VendorStatus.Pending && Status != VendorStatus.Rejected)
            throw new DomainException("Vendor cannot be submitted for approval");

        if (!_documents.Any(d => d.Type == DocumentType.BusinessLicense && d.IsVerified))
            throw new DomainException("Business license must be uploaded and verified");

        Status = VendorStatus.UnderReview;
    }

    public void Approve(Guid approvedBy, string? notes = null)
    {
        if (Status != VendorStatus.UnderReview)
            throw new DomainException("Vendor is not under review");

        Status = VendorStatus.Active;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;

        if (!string.IsNullOrWhiteSpace(notes))
            AddNote(approvedBy, notes, VendorNoteType.Approval);
    }

    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status != VendorStatus.UnderReview)
            throw new DomainException("Vendor is not under review");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required");

        Status = VendorStatus.Rejected;
        AddNote(rejectedBy, reason, VendorNoteType.Rejection);
    }

    public void Suspend(Guid suspendedBy, string reason)
    {
        if (!IsActive)
            throw new DomainException("Only active vendors can be suspended");

        Status = VendorStatus.Suspended;
        AddNote(suspendedBy, reason, VendorNoteType.Suspension);
    }

    public void Reactivate(Guid reactivatedBy, string? notes = null)
    {
        if (Status != VendorStatus.Suspended)
            throw new DomainException("Only suspended vendors can be reactivated");

        Status = VendorStatus.Active;
        AddNote(reactivatedBy, notes ?? "Vendor reactivated", VendorNoteType.Reactivation);
    }

    public void Terminate(Guid terminatedBy, string reason)
    {
        Status = VendorStatus.Terminated;
        AddNote(terminatedBy, $"Terminated: {reason}", VendorNoteType.Termination);
    }

    #endregion

    #region Category Management

    public VendorCategory AddCategory(Guid categoryId, string categoryName, decimal? customCommissionRate = null)
    {
        if (_categories.Any(c => c.CategoryId == categoryId))
            throw new DomainException("Category already added");

        var category = new VendorCategory(
            Guid.NewGuid(),
            categoryId,
            categoryName,
            customCommissionRate ?? DefaultCommissionRate);

        _categories.Add(category);
        return category;
    }

    public void RemoveCategory(Guid categoryId)
    {
        var category = _categories.FirstOrDefault(c => c.CategoryId == categoryId)
            ?? throw new DomainException("Category not found");

        _categories.Remove(category);
    }

    public void ApproveCategory(Guid categoryId, Guid approvedBy)
    {
        var category = _categories.FirstOrDefault(c => c.CategoryId == categoryId)
            ?? throw new DomainException("Category not found");

        category.Approve(approvedBy);
    }

    public void UpdateCategoryCommission(Guid categoryId, decimal commissionRate)
    {
        var category = _categories.FirstOrDefault(c => c.CategoryId == categoryId)
            ?? throw new DomainException("Category not found");

        category.UpdateCommissionRate(commissionRate);
    }

    #endregion

    #region Document Management

    public VendorDocument AddDocument(DocumentType type, string documentUrl, string? description = null)
    {
        var document = new VendorDocument(Guid.NewGuid(), type, documentUrl, description);
        _documents.Add(document);
        return document;
    }

    public void VerifyDocument(Guid documentId, Guid verifiedBy)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new DomainException("Document not found");

        document.Verify(verifiedBy);
    }

    public void RejectDocument(Guid documentId, string reason)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new DomainException("Document not found");

        document.Reject(reason);
    }

    #endregion

    #region Financial

    public void SetBankDetails(string bankName, string accountNumber)
    {
        BankName = bankName;
        BankAccountNumber = accountNumber;
    }

    public void UpdatePayoutSchedule(PayoutSchedule schedule, int minimumAmount)
    {
        if (minimumAmount < 10)
            throw new DomainException("Minimum payout amount must be at least 10");

        PayoutSchedule = schedule;
        MinimumPayoutAmount = minimumAmount;
    }

    public void UpdateCommissionRate(decimal rate)
    {
        if (rate < 0 || rate > 50)
            throw new DomainException("Commission rate must be between 0% and 50%");

        DefaultCommissionRate = rate;
    }

    public VendorPayout CreatePayout(Money amount, DateTime periodStart, DateTime periodEnd)
    {
        if (string.IsNullOrEmpty(BankAccountNumber))
            throw new DomainException("Bank details must be configured for payouts");

        if (amount.Amount < MinimumPayoutAmount)
            throw new DomainException($"Payout amount must be at least {MinimumPayoutAmount}");

        var payout = new VendorPayout(
            Guid.NewGuid(),
            amount,
            periodStart,
            periodEnd);

        _payouts.Add(payout);
        return payout;
    }

    public void ProcessPayout(Guid payoutId, string transactionReference)
    {
        var payout = _payouts.FirstOrDefault(p => p.Id == payoutId)
            ?? throw new DomainException("Payout not found");

        payout.Process(transactionReference);
    }

    #endregion

    #region Performance Tracking

    public void RecordSale(int productsSold, Money revenue)
    {
        TotalOrders++;
        TotalProductsSold += productsSold;
        TotalRevenue = TotalRevenue.Add(revenue);
        LastActiveAt = DateTime.UtcNow;
        UpdateTier();
    }

    public void RecordReview(int rating)
    {
        var totalRatingSum = AverageRating * TotalReviews + rating;
        TotalReviews++;
        AverageRating = Math.Round(totalRatingSum / TotalReviews, 2);
    }

    public void UpdatePerformanceMetrics(
        decimal fulfillmentRate,
        decimal onTimeRate,
        decimal returnRate)
    {
        OrderFulfillmentRate = Math.Clamp(fulfillmentRate, 0, 100);
        OnTimeShippingRate = Math.Clamp(onTimeRate, 0, 100);
        ReturnRate = Math.Clamp(returnRate, 0, 100);
        UpdateTier();
    }

    private void UpdateTier()
    {
        // Tier based on revenue and performance
        var newTier = (TotalRevenue.Amount, AverageRating, OnTimeShippingRate) switch
        {
            ( >= 100000, >= 4.5m, >= 95) => VendorTier.Platinum,
            ( >= 50000, >= 4.0m, >= 90) => VendorTier.Gold,
            ( >= 10000, >= 3.5m, >= 85) => VendorTier.Silver,
            _ => VendorTier.Standard
        };

        if (newTier > Tier)
        {
            Tier = newTier;
            // Reduce commission for higher tiers
            DefaultCommissionRate = newTier switch
            {
                VendorTier.Platinum => 8,
                VendorTier.Gold => 10,
                VendorTier.Silver => 12,
                _ => 15
            };
        }
    }

    #endregion

    #region Profile Updates

    public void UpdateProfile(string businessName, string? description, string? logoUrl, string? bannerUrl)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new DomainException("Business name is required");

        BusinessName = businessName;
        Description = description;
        LogoUrl = logoUrl;
        BannerUrl = bannerUrl;
    }

    public void UpdateContact(string email, string phone, Address address)
    {
        ContactEmail = email.ToLowerInvariant();
        ContactPhone = phone;
        BusinessAddress = address;
    }

    private void AddNote(Guid userId, string content, VendorNoteType type)
    {
        _notes.Add(new VendorNote(Guid.NewGuid(), userId, content, type));
    }

    #endregion

    private static string GenerateVendorCode()
    {
        return $"V{DateTime.UtcNow:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

public class VendorCategory : Entity<Guid>
{
    public Guid CategoryId { get; private set; }
    public string CategoryName { get; private set; }
    public decimal CommissionRate { get; private set; }
    public bool IsApproved { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    internal VendorCategory(Guid id, Guid categoryId, string categoryName, decimal commissionRate)
    {
        Id = id;
        CategoryId = categoryId;
        CategoryName = categoryName;
        CommissionRate = commissionRate;
        IsApproved = false;
    }

    private VendorCategory() { }

    internal void Approve(Guid approvedBy)
    {
        IsApproved = true;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    internal void UpdateCommissionRate(decimal rate)
    {
        if (rate < 0 || rate > 50)
            throw new DomainException("Commission rate must be between 0% and 50%");
        CommissionRate = rate;
    }
}

public class VendorDocument : Entity<Guid>
{
    public DocumentType Type { get; private set; }
    public string DocumentUrl { get; private set; }
    public string? Description { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public bool IsVerified => Status == DocumentStatus.Verified;

    internal VendorDocument(Guid id, DocumentType type, string documentUrl, string? description)
    {
        Id = id;
        Type = type;
        DocumentUrl = documentUrl;
        Description = description;
        Status = DocumentStatus.Pending;
        UploadedAt = DateTime.UtcNow;
    }

    private VendorDocument() { }

    internal void Verify(Guid verifiedBy)
    {
        Status = DocumentStatus.Verified;
        VerifiedBy = verifiedBy;
        VerifiedAt = DateTime.UtcNow;
    }

    internal void Reject(string reason)
    {
        Status = DocumentStatus.Rejected;
        RejectionReason = reason;
    }
}

public class VendorPayout : Entity<Guid>
{
    public Money Amount { get; private set; }
    public PayoutStatus Status { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public string? TransactionReference { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    internal VendorPayout(Guid id, Money amount, DateTime periodStart, DateTime periodEnd)
    {
        Id = id;
        Amount = amount;
        Status = PayoutStatus.Pending;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        CreatedAt = DateTime.UtcNow;
    }

    private VendorPayout() { }

    internal void Process(string transactionReference)
    {
        Status = PayoutStatus.Processed;
        TransactionReference = transactionReference;
        ProcessedAt = DateTime.UtcNow;
    }

    internal void Fail(string reason)
    {
        Status = PayoutStatus.Failed;
    }
}

public class VendorNote : Entity<Guid>
{
    public Guid CreatedBy { get; private set; }
    public string Content { get; private set; }
    public VendorNoteType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal VendorNote(Guid id, Guid createdBy, string content, VendorNoteType type)
    {
        Id = id;
        CreatedBy = createdBy;
        Content = content;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    private VendorNote() { }
}

public enum VendorStatus
{
    Pending = 1,
    UnderReview = 2,
    Active = 3,
    Suspended = 4,
    Rejected = 5,
    Terminated = 6
}

public enum VendorTier
{
    Standard = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4
}

public enum PayoutSchedule
{
    Weekly = 1,
    BiWeekly = 2,
    Monthly = 3
}

public enum PayoutStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4
}

public enum DocumentType
{
    BusinessLicense = 1,
    TaxCertificate = 2,
    BankStatement = 3,
    IdentityDocument = 4,
    AddressProof = 5,
    ProductCertification = 6
}

public enum DocumentStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3,
    Expired = 4
}

public enum VendorNoteType
{
    General = 1,
    Approval = 2,
    Rejection = 3,
    Suspension = 4,
    Reactivation = 5,
    Termination = 6,
    Warning = 7
}
