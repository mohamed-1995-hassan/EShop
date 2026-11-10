using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Identity;

/// <summary>
/// Password hashing service using PBKDF2
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    PasswordValidationResult ValidatePassword(string password);
}

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 100000; // OWASP recommendation
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    private readonly PasswordSettings _settings;

    public PasswordHasher(IOptions<PasswordSettings> settings)
    {
        _settings = settings.Value;
    }

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        // Format: iterations.algorithm.salt.hash
        return $"{Iterations}.{Algorithm.Name}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 4) return false;

            var iterations = int.Parse(parts[0]);
            var algorithm = new HashAlgorithmName(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var hash = Convert.FromBase64String(parts[3]);

            var newHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, hash.Length);

            return CryptographicOperations.FixedTimeEquals(hash, newHash);
        }
        catch
        {
            return false;
        }
    }

    public PasswordValidationResult ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return new PasswordValidationResult(false, errors);
        }

        if (password.Length < _settings.MinimumLength)
        {
            errors.Add($"Password must be at least {_settings.MinimumLength} characters long");
        }

        if (_settings.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (_settings.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (_settings.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (_settings.RequireSpecialCharacter && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain at least one special character");
        }

        // Check for common passwords (basic check)
        var commonPasswords = new[] { "password", "123456", "qwerty", "admin", "letmein", "welcome" };
        if (commonPasswords.Any(p => password.ToLower().Contains(p)))
        {
            errors.Add("Password is too common");
        }

        return new PasswordValidationResult(errors.Count == 0, errors);
    }
}

public record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors);
