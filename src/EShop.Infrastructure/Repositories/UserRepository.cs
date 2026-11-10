using EShop.Domain.Aggregates.IdentityAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

/// <summary>
/// User repository implementation
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly EShopDbContext _context;

    public UserRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .Include(u => u.ExternalLogins)
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .Include(u => u.ExternalLogins)
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .Include(u => u.ExternalLogins)
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken), cancellationToken);
    }

    public async Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .Include(u => u.ExternalLogins)
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.ExternalLogins.Any(l => 
                l.Provider == provider && l.ProviderKey == providerKey), cancellationToken);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .Where(u => u.Roles.Any(r => r.Role == role))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Email verification token repository
/// </summary>
public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly EShopDbContext _context;

    public EmailVerificationTokenRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        _context.EmailVerificationTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _context.EmailVerificationTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.EmailVerificationTokens.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Password reset token repository
/// </summary>
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly EShopDbContext _context;

    public PasswordResetTokenRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _context.PasswordResetTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.PasswordResetTokens.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
