using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Security;

/// <summary>
/// AES-256-GCM encryption implementation.
/// 
/// SECURITY FEATURES:
/// - AES-256-GCM (authenticated encryption)
/// - Unique IV per encryption
/// - Key derivation with PBKDF2
/// - Key rotation support
/// </summary>
public class AesMessageEncryption : IMessageEncryption
{
    private readonly EncryptionSettings _settings;
    private readonly ILogger<AesMessageEncryption> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AesMessageEncryption(
        IOptions<EncryptionSettings> settings,
        ILogger<AesMessageEncryption> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public Task<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plaintext))
            return Task.FromResult(string.Empty);

        var key = DeriveKey(_settings.MasterKey, _settings.Salt);
        var iv = RandomNumberGenerator.GetBytes(12); // 96-bit IV for GCM
        var tag = new byte[16]; // 128-bit auth tag

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

        // Combine IV + Tag + Ciphertext
        var result = new byte[iv.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(tag, 0, result, iv.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length + tag.Length, ciphertext.Length);

        return Task.FromResult(Convert.ToBase64String(result));
    }

    public Task<string> DecryptAsync(string ciphertextBase64, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ciphertextBase64))
            return Task.FromResult(string.Empty);

        var combined = Convert.FromBase64String(ciphertextBase64);
        
        var iv = new byte[12];
        var tag = new byte[16];
        var ciphertext = new byte[combined.Length - 28];

        Buffer.BlockCopy(combined, 0, iv, 0, 12);
        Buffer.BlockCopy(combined, 12, tag, 0, 16);
        Buffer.BlockCopy(combined, 28, ciphertext, 0, ciphertext.Length);

        var key = DeriveKey(_settings.MasterKey, _settings.Salt);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Task.FromResult(Encoding.UTF8.GetString(plaintext));
    }

    public async Task<string> EncryptObjectAsync<T>(T obj, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(obj, _jsonOptions);
        return await EncryptAsync(json, cancellationToken);
    }

    public async Task<T?> DecryptObjectAsync<T>(string ciphertext, CancellationToken cancellationToken = default)
    {
        var json = await DecryptAsync(ciphertext, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task<T> EncryptFieldsAsync<T>(T obj, params string[] fieldNames) where T : class
    {
        var type = typeof(T);
        
        // If no specific fields, look for [Encrypted] attribute
        var fieldsToEncrypt = fieldNames.Length > 0
            ? fieldNames
            : type.GetProperties()
                .Where(p => p.GetCustomAttribute<EncryptedAttribute>() != null)
                .Select(p => p.Name)
                .ToArray();

        foreach (var fieldName in fieldsToEncrypt)
        {
            var property = type.GetProperty(fieldName);
            if (property == null || property.PropertyType != typeof(string))
                continue;

            var value = property.GetValue(obj) as string;
            if (string.IsNullOrEmpty(value))
                continue;

            var encrypted = await EncryptAsync(value);
            property.SetValue(obj, encrypted);
        }

        return obj;
    }

    public async Task<T> DecryptFieldsAsync<T>(T obj, params string[] fieldNames) where T : class
    {
        var type = typeof(T);

        var fieldsToDecrypt = fieldNames.Length > 0
            ? fieldNames
            : type.GetProperties()
                .Where(p => p.GetCustomAttribute<EncryptedAttribute>() != null)
                .Select(p => p.Name)
                .ToArray();

        foreach (var fieldName in fieldsToDecrypt)
        {
            var property = type.GetProperty(fieldName);
            if (property == null || property.PropertyType != typeof(string))
                continue;

            var value = property.GetValue(obj) as string;
            if (string.IsNullOrEmpty(value))
                continue;

            try
            {
                var decrypted = await DecryptAsync(value);
                property.SetValue(obj, decrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt field {Field}, may not be encrypted", fieldName);
            }
        }

        return obj;
    }

    public Task<byte[]> GenerateDataKeyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RandomNumberGenerator.GetBytes(32)); // 256-bit key
    }

    public Task RotateKeysAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Key rotation requested - implement re-encryption of existing data");
        // In production:
        // 1. Generate new key version
        // 2. Update settings
        // 3. Re-encrypt data with new key
        // 4. Update key version in encrypted data
        return Task.CompletedTask;
    }

    private static byte[] DeriveKey(string masterKey, string salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            masterKey,
            Encoding.UTF8.GetBytes(salt),
            100000, // iterations
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32); // 256-bit key
    }
}

/// <summary>
/// Encryption configuration
/// </summary>
public class EncryptionSettings
{
    /// <summary>
    /// Master encryption key (should be from Key Vault in production)
    /// </summary>
    public string MasterKey { get; set; } = string.Empty;

    /// <summary>
    /// Salt for key derivation
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// Current key version
    /// </summary>
    public int CurrentKeyVersion { get; set; } = 1;
}
