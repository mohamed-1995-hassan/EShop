namespace EShop.Infrastructure.Security;

/// <summary>
/// MESSAGE ENCRYPTION SERVICE
/// 
/// Encrypts sensitive data in messages for:
/// - Payment information
/// - Personal data (PII)
/// - Health information
/// 
/// ENCRYPTION STRATEGY:
/// - Field-level encryption (encrypt specific fields)
/// - Envelope encryption (encrypt payload, key in header)
/// - Key rotation support
/// </summary>
public interface IMessageEncryption
{
    /// <summary>
    /// Encrypt a string value
    /// </summary>
    Task<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt a string value
    /// </summary>
    Task<string> DecryptAsync(string ciphertext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypt an object (serializes and encrypts)
    /// </summary>
    Task<string> EncryptObjectAsync<T>(T obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt an object
    /// </summary>
    Task<T?> DecryptObjectAsync<T>(string ciphertext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypt specific fields in an object
    /// </summary>
    Task<T> EncryptFieldsAsync<T>(T obj, params string[] fieldNames) where T : class;

    /// <summary>
    /// Decrypt specific fields in an object
    /// </summary>
    Task<T> DecryptFieldsAsync<T>(T obj, params string[] fieldNames) where T : class;

    /// <summary>
    /// Generate a data encryption key
    /// </summary>
    Task<byte[]> GenerateDataKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotate encryption keys
    /// </summary>
    Task RotateKeysAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Envelope encryption result
/// </summary>
public class EncryptedEnvelope
{
    /// <summary>
    /// Encrypted data
    /// </summary>
    public string Ciphertext { get; init; } = string.Empty;

    /// <summary>
    /// Encrypted data key (encrypted with master key)
    /// </summary>
    public string EncryptedDataKey { get; init; } = string.Empty;

    /// <summary>
    /// Key version for rotation
    /// </summary>
    public int KeyVersion { get; init; }

    /// <summary>
    /// Initialization vector
    /// </summary>
    public string IV { get; init; } = string.Empty;

    /// <summary>
    /// Algorithm used
    /// </summary>
    public string Algorithm { get; init; } = "AES-256-GCM";
}

/// <summary>
/// Attribute to mark encrypted fields
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
}

/// <summary>
/// Attribute to mark PII fields (for compliance)
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PiiAttribute : Attribute
{
}
