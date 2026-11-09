using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.DigitalProductAggregate;

/// <summary>
/// Digital Product Aggregate - Downloadable products and license keys
/// 
/// BUSINESS RULES:
/// - License keys generated or imported
/// - Download limits and expiry periods
/// - Multiple file versions supported
/// - Access revocation for refunds
/// - Watermarking for traceability (optional)
/// - Regional restrictions possible
/// </summary>
public class DigitalProduct : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DigitalProductType Type { get; private set; }
    public DeliveryMethod DeliveryMethod { get; private set; }
    public bool RequiresLicense { get; private set; }
    public LicenseType? LicenseType { get; private set; }
    public int? MaxDownloads { get; private set; }
    public int? MaxDevices { get; private set; }
    public int? AccessDurationDays { get; private set; }
    public bool AllowWatermarking { get; private set; }
    public string? WatermarkTemplate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<DigitalFile> _files = new();
    public IReadOnlyCollection<DigitalFile> Files => _files.AsReadOnly();

    private readonly List<LicenseKey> _licenseKeys = new();
    public IReadOnlyCollection<LicenseKey> LicenseKeys => _licenseKeys.AsReadOnly();

    private readonly List<string> _restrictedCountries = new();
    public IReadOnlyCollection<string> RestrictedCountries => _restrictedCountries.AsReadOnly();

    public int AvailableLicenseCount => _licenseKeys.Count(k => !k.IsAssigned);
    public bool HasAvailableLicenses => !RequiresLicense || AvailableLicenseCount > 0;

    private DigitalProduct() { }

    public static DigitalProduct CreateDownloadable(
        Guid productId,
        string name,
        string description,
        int? maxDownloads = 5,
        int? accessDurationDays = null)
    {
        return Create(productId, name, description, DigitalProductType.Download, 
            DeliveryMethod.DirectDownload, false, null, maxDownloads, null, accessDurationDays);
    }

    public static DigitalProduct CreateSoftware(
        Guid productId,
        string name,
        string description,
        LicenseType licenseType,
        int? maxDevices = 1)
    {
        return Create(productId, name, description, DigitalProductType.Software,
            DeliveryMethod.DirectDownload, true, licenseType, null, maxDevices, null);
    }

    public static DigitalProduct CreateSubscriptionContent(
        Guid productId,
        string name,
        string description,
        int accessDurationDays)
    {
        return Create(productId, name, description, DigitalProductType.Subscription,
            DeliveryMethod.Streaming, false, null, null, null, accessDurationDays);
    }

    public static DigitalProduct CreateEbook(
        Guid productId,
        string name,
        string description,
        bool allowWatermarking = true)
    {
        var product = Create(productId, name, description, DigitalProductType.Ebook,
            DeliveryMethod.DirectDownload, false, null, 10, 5, null);
        product.AllowWatermarking = allowWatermarking;
        return product;
    }

    private static DigitalProduct Create(
        Guid productId,
        string name,
        string description,
        DigitalProductType type,
        DeliveryMethod deliveryMethod,
        bool requiresLicense,
        LicenseType? licenseType,
        int? maxDownloads,
        int? maxDevices,
        int? accessDurationDays)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");

        return new DigitalProduct
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = name,
            Description = description,
            Type = type,
            DeliveryMethod = deliveryMethod,
            RequiresLicense = requiresLicense,
            LicenseType = licenseType,
            MaxDownloads = maxDownloads,
            MaxDevices = maxDevices,
            AccessDurationDays = accessDurationDays,
            AllowWatermarking = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region File Management

    public DigitalFile AddFile(
        string fileName,
        string fileUrl,
        long fileSizeBytes,
        string contentType,
        string version = "1.0",
        string? checksum = null,
        FileType fileType = FileType.Primary)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("File name is required");
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new DomainException("File URL is required");

        var file = new DigitalFile(
            Guid.NewGuid(),
            fileName,
            fileUrl,
            fileSizeBytes,
            contentType,
            version,
            checksum,
            fileType,
            _files.Count == 0);

        _files.Add(file);
        return file;
    }

    public void RemoveFile(Guid fileId)
    {
        var file = _files.FirstOrDefault(f => f.Id == fileId)
            ?? throw new DomainException("File not found");

        _files.Remove(file);

        // Ensure there's still a primary file
        if (file.IsPrimary && _files.Any())
            _files[0].SetAsPrimary();
    }

    public void SetPrimaryFile(Guid fileId)
    {
        var file = _files.FirstOrDefault(f => f.Id == fileId)
            ?? throw new DomainException("File not found");

        foreach (var f in _files)
            f.ClearPrimary();

        file.SetAsPrimary();
    }

    public void UpdateFileVersion(Guid fileId, string newVersion, string newUrl, long newSize, string? checksum = null)
    {
        var file = _files.FirstOrDefault(f => f.Id == fileId)
            ?? throw new DomainException("File not found");

        file.UpdateVersion(newVersion, newUrl, newSize, checksum);
    }

    public DigitalFile? GetLatestFile(FileType? type = null)
    {
        var query = _files.Where(f => f.IsActive);
        if (type.HasValue)
            query = query.Where(f => f.FileType == type.Value);

        return query
            .OrderByDescending(f => f.IsPrimary)
            .ThenByDescending(f => f.Version)
            .FirstOrDefault();
    }

    #endregion

    #region License Key Management

    public LicenseKey AddLicenseKey(string key, DateTime? expiresAt = null)
    {
        if (!RequiresLicense)
            throw new DomainException("This product does not require licenses");

        if (_licenseKeys.Any(k => k.Key == key))
            throw new DomainException("License key already exists");

        var license = new LicenseKey(
            Guid.NewGuid(),
            key,
            LicenseType ?? Domain.Aggregates.DigitalProductAggregate.LicenseType.Perpetual,
            MaxDevices ?? 1,
            expiresAt);

        _licenseKeys.Add(license);
        return license;
    }

    public void ImportLicenseKeys(IEnumerable<string> keys, DateTime? expiresAt = null)
    {
        foreach (var key in keys.Distinct())
        {
            if (!_licenseKeys.Any(k => k.Key == key))
            {
                _licenseKeys.Add(new LicenseKey(
                    Guid.NewGuid(),
                    key,
                    LicenseType ?? Domain.Aggregates.DigitalProductAggregate.LicenseType.Perpetual,
                    MaxDevices ?? 1,
                    expiresAt));
            }
        }
    }

    public LicenseKey? AssignLicenseKey(Guid customerId, Guid orderId)
    {
        if (!RequiresLicense)
            return null;

        var availableKey = _licenseKeys.FirstOrDefault(k => !k.IsAssigned)
            ?? throw new DomainException("No license keys available");

        availableKey.AssignTo(customerId, orderId);
        return availableKey;
    }

    public void RevokeLicense(Guid licenseId, string reason)
    {
        var license = _licenseKeys.FirstOrDefault(k => k.Id == licenseId)
            ?? throw new DomainException("License not found");

        license.Revoke(reason);
    }

    public LicenseValidationResult ValidateLicense(string key, string? deviceId = null)
    {
        var license = _licenseKeys.FirstOrDefault(k => k.Key == key);

        if (license == null)
            return LicenseValidationResult.Invalid("License key not found");

        if (!license.IsAssigned)
            return LicenseValidationResult.Invalid("License key not activated");

        if (license.IsRevoked)
            return LicenseValidationResult.Invalid($"License revoked: {license.RevocationReason}");

        if (license.ExpiresAt.HasValue && DateTime.UtcNow > license.ExpiresAt.Value)
            return LicenseValidationResult.Invalid("License has expired");

        if (deviceId != null)
        {
            if (!license.IsDeviceAuthorized(deviceId) && !license.CanAddDevice())
                return LicenseValidationResult.Invalid($"Maximum devices ({license.MaxDevices}) reached");

            if (!license.IsDeviceAuthorized(deviceId))
                license.AuthorizeDevice(deviceId);
        }

        return LicenseValidationResult.Valid(license);
    }

    #endregion

    #region Access Control

    public void AddCountryRestriction(string countryCode)
    {
        var code = countryCode.ToUpperInvariant();
        if (!_restrictedCountries.Contains(code))
            _restrictedCountries.Add(code);
    }

    public void RemoveCountryRestriction(string countryCode)
    {
        _restrictedCountries.Remove(countryCode.ToUpperInvariant());
    }

    public bool IsAvailableInCountry(string countryCode)
    {
        return !_restrictedCountries.Contains(countryCode.ToUpperInvariant());
    }

    public void SetWatermarkTemplate(string template)
    {
        if (!AllowWatermarking)
            throw new DomainException("Watermarking not enabled for this product");

        WatermarkTemplate = template;
    }

    #endregion

    #region Configuration

    public void UpdateAccessSettings(int? maxDownloads, int? maxDevices, int? accessDurationDays)
    {
        MaxDownloads = maxDownloads;
        MaxDevices = maxDevices;
        AccessDurationDays = accessDurationDays;

        // Update existing unassigned licenses
        foreach (var license in _licenseKeys.Where(k => !k.IsAssigned))
        {
            license.UpdateMaxDevices(maxDevices ?? 1);
        }
    }

    public void EnableWatermarking(string template)
    {
        AllowWatermarking = true;
        WatermarkTemplate = template;
    }

    public void DisableWatermarking()
    {
        AllowWatermarking = false;
        WatermarkTemplate = null;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    #endregion
}

public class DigitalFile : Entity<Guid>
{
    public string FileName { get; private set; }
    public string FileUrl { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string ContentType { get; private set; }
    public string Version { get; private set; }
    public string? Checksum { get; private set; }
    public FileType FileType { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public string FileSizeFormatted => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    internal DigitalFile(
        Guid id,
        string fileName,
        string fileUrl,
        long fileSizeBytes,
        string contentType,
        string version,
        string? checksum,
        FileType fileType,
        bool isPrimary)
    {
        Id = id;
        FileName = fileName;
        FileUrl = fileUrl;
        FileSizeBytes = fileSizeBytes;
        ContentType = contentType;
        Version = version;
        Checksum = checksum;
        FileType = fileType;
        IsPrimary = isPrimary;
        IsActive = true;
        UploadedAt = DateTime.UtcNow;
    }

    private DigitalFile() { }

    internal void UpdateVersion(string newVersion, string newUrl, long newSize, string? checksum)
    {
        Version = newVersion;
        FileUrl = newUrl;
        FileSizeBytes = newSize;
        Checksum = checksum;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void SetAsPrimary() => IsPrimary = true;
    internal void ClearPrimary() => IsPrimary = false;
    internal void Deactivate() => IsActive = false;
}

public class LicenseKey : Entity<Guid>
{
    public string Key { get; private set; }
    public LicenseType Type { get; private set; }
    public int MaxDevices { get; private set; }
    public bool IsAssigned { get; private set; }
    public Guid? AssignedToCustomerId { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? RevocationReason { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<AuthorizedDevice> _devices = new();
    public IReadOnlyCollection<AuthorizedDevice> Devices => _devices.AsReadOnly();

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool IsValid => IsAssigned && !IsRevoked && !IsExpired;

    internal LicenseKey(
        Guid id,
        string key,
        LicenseType type,
        int maxDevices,
        DateTime? expiresAt)
    {
        Id = id;
        Key = key;
        Type = type;
        MaxDevices = maxDevices;
        ExpiresAt = expiresAt;
        IsAssigned = false;
        IsRevoked = false;
        CreatedAt = DateTime.UtcNow;
    }

    private LicenseKey() { }

    internal void AssignTo(Guid customerId, Guid orderId)
    {
        if (IsAssigned)
            throw new DomainException("License already assigned");

        IsAssigned = true;
        AssignedToCustomerId = customerId;
        OrderId = orderId;
        AssignedAt = DateTime.UtcNow;
    }

    internal void Revoke(string reason)
    {
        IsRevoked = true;
        RevocationReason = reason;
        RevokedAt = DateTime.UtcNow;
        
        // Deauthorize all devices
        foreach (var device in _devices)
            device.Deauthorize();
    }

    internal void UpdateMaxDevices(int maxDevices) => MaxDevices = maxDevices;

    internal bool CanAddDevice() => _devices.Count(d => d.IsAuthorized) < MaxDevices;

    internal bool IsDeviceAuthorized(string deviceId)
    {
        return _devices.Any(d => d.DeviceId == deviceId && d.IsAuthorized);
    }

    internal void AuthorizeDevice(string deviceId, string? deviceName = null)
    {
        if (!CanAddDevice())
            throw new DomainException("Maximum devices reached");

        var existing = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (existing != null)
        {
            existing.Reauthorize();
        }
        else
        {
            _devices.Add(new AuthorizedDevice(Guid.NewGuid(), deviceId, deviceName));
        }
    }

    internal void DeauthorizeDevice(string deviceId)
    {
        var device = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
        device?.Deauthorize();
    }
}

public class AuthorizedDevice : Entity<Guid>
{
    public string DeviceId { get; private set; }
    public string? DeviceName { get; private set; }
    public bool IsAuthorized { get; private set; }
    public DateTime AuthorizedAt { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }

    internal AuthorizedDevice(Guid id, string deviceId, string? deviceName)
    {
        Id = id;
        DeviceId = deviceId;
        DeviceName = deviceName;
        IsAuthorized = true;
        AuthorizedAt = DateTime.UtcNow;
    }

    private AuthorizedDevice() { }

    internal void Deauthorize() => IsAuthorized = false;
    internal void Reauthorize()
    {
        IsAuthorized = true;
        AuthorizedAt = DateTime.UtcNow;
    }
    internal void RecordAccess() => LastAccessedAt = DateTime.UtcNow;
}

public class LicenseValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public LicenseKey? License { get; }

    private LicenseValidationResult(bool isValid, string? errorMessage, LicenseKey? license)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        License = license;
    }

    public static LicenseValidationResult Valid(LicenseKey license) => new(true, null, license);
    public static LicenseValidationResult Invalid(string error) => new(false, error, null);
}

public enum DigitalProductType
{
    Download = 1,       // One-time download (music, documents)
    Software = 2,       // Requires license activation
    Ebook = 3,          // Digital books
    Course = 4,         // Online courses
    Subscription = 5,   // Ongoing access
    GameKey = 6,        // Game activation codes
    Template = 7,       // Design templates
    Font = 8            // Digital fonts
}

public enum DeliveryMethod
{
    DirectDownload = 1, // Direct file download
    Email = 2,          // Sent via email
    Streaming = 3,      // Online streaming
    ApiKey = 4,         // API access key
    CloudAccess = 5     // Cloud storage access
}

public enum LicenseType
{
    Perpetual = 1,      // Never expires
    Subscription = 2,   // Time-limited
    Trial = 3,          // Free trial
    Educational = 4,    // Academic license
    Enterprise = 5,     // Business/volume license
    OEM = 6             // Pre-installed
}

public enum FileType
{
    Primary = 1,        // Main downloadable
    Installer = 2,      // Software installer
    Update = 3,         // Update/patch
    Documentation = 4,  // User manual
    Sample = 5,         // Sample/preview
    Bonus = 6           // Bonus content
}
