using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using EShop.Domain.Aggregates.IdentityAggregate;
using EShop.Infrastructure.Identity;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Admin;

/// <summary>
/// Implementation of admin user management service
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly EShopDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        EShopDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<AdminUserService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        bool? isActive = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Roles.Any(r => r.Role == role));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "email" => sortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "lastlogin" => sortDescending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
            _ => sortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };

        var users = await query
            .Include(u => u.Roles)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = users.Select(MapToAdminUserDto).ToList();

        return PagedResult<AdminUserDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<AdminUserDetailDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return null;

        return MapToAdminUserDetailDto(user);
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
            throw new InvalidOperationException($"User with email {request.Email} already exists");

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user with password hash and roles
        var user = User.Create(request.Email, passwordHash, request.Roles.ToArray());

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created: {UserId} - {Email}", user.Id, user.Email);

        return MapToAdminUserDto(user);
    }

    public async Task<AdminUserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                user.Reactivate();
            else
                user.Deactivate();
        }

        if (request.Roles != null)
        {
            // Clear existing roles and add new ones
            foreach (var existingRole in user.Roles.ToList())
            {
                user.RemoveRole(existingRole.Role);
            }
            foreach (var roleName in request.Roles)
            {
                user.AddRole(roleName);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User updated: {UserId}", userId);

        return MapToAdminUserDto(user);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        // Soft delete
        user.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User soft-deleted: {UserId}", userId);
    }

    public async Task<AdminUserDto> UpdateUserRolesAsync(Guid userId, UserRoleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        // Clear existing roles and add new ones
        foreach (var existingRole in user.Roles.ToList())
        {
            user.RemoveRole(existingRole.Role);
        }
        foreach (var roleName in request.Roles)
        {
            user.AddRole(roleName);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User roles updated: {UserId}", userId);

        return MapToAdminUserDto(user);
    }

    public async Task LockUserAsync(Guid userId, DateTime? lockoutEnd = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        // Lock by recording enough failed login attempts
        for (int i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(5, (int)(lockoutEnd?.Subtract(DateTime.UtcNow).TotalMinutes ?? 43200)); // Default 30 days
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User locked: {UserId} until {LockoutEnd}", userId, lockoutEnd);
    }

    public async Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        user.Unlock();
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User unlocked: {UserId}", userId);
    }

    public async Task ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        // Generate a temporary password
        var tempPassword = GenerateTemporaryPassword();
        var passwordHash = _passwordHasher.HashPassword(tempPassword);
        user.ChangePassword(passwordHash);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Password reset for user: {UserId}", userId);

        // In a real implementation, you would send an email with the temp password
        // or a reset link
    }

    public async Task TerminateUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        // Revoke all refresh tokens
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoke("Session terminated by admin");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("All sessions terminated for user: {UserId}", userId);
    }

    public Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default)
    {
        // Return predefined roles
        IReadOnlyList<string> roles = new List<string>
        {
            "Admin",
            "Manager",
            "Staff",
            "Customer",
            "Guest"
        };
        return Task.FromResult(roles);
    }

    public async Task<BulkOperationResultDto> BulkDeleteUsersAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var userId in userIds)
        {
            try
            {
                await DeleteUserAsync(userId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = userId.ToString(), ErrorMessage = ex.Message });
            }
        }

        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    public async Task<BulkOperationResultDto> BulkActivateUsersAsync(IReadOnlyList<Guid> userIds, bool activate, CancellationToken cancellationToken = default)
    {
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var userId in userIds)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (user == null)
                {
                    errors.Add(new BulkOperationError { ItemId = userId.ToString(), ErrorMessage = "User not found" });
                    continue;
                }

                if (activate)
                    user.Reactivate();
                else
                    user.Deactivate();

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = userId.ToString(), ErrorMessage = ex.Message });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    private static AdminUserDto MapToAdminUserDto(User user)
    {
        // User entity doesn't have first/last name, extract from email or use default
        var emailParts = user.Email.Split('@');
        var namePart = emailParts[0];
        
        return new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = namePart,
            LastName = "",
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Roles = user.Roles.Select(r => r.Role).ToList(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            FailedLoginAttempts = user.AccessFailedCount,
            LockoutEnd = user.LockoutEnd
        };
    }

    private static AdminUserDetailDto MapToAdminUserDetailDto(User user)
    {
        var dto = MapToAdminUserDto(user);
        return new AdminUserDetailDto
        {
            Id = dto.Id,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            IsActive = dto.IsActive,
            EmailConfirmed = dto.EmailConfirmed,
            TwoFactorEnabled = dto.TwoFactorEnabled,
            Roles = dto.Roles,
            CreatedAt = dto.CreatedAt,
            LastLoginAt = dto.LastLoginAt,
            FailedLoginAttempts = dto.FailedLoginAttempts,
            LockoutEnd = dto.LockoutEnd,
            ActiveSessions = user.RefreshTokens
                .Where(t => !t.IsRevoked && !t.IsExpired)
                .Select(t => new UserSessionDto
                {
                    SessionId = Guid.NewGuid().ToString(),
                    IpAddress = t.CreatedByIp,
                    UserAgent = t.UserAgent ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    LastActivityAt = t.CreatedAt
                })
                .ToList(),
            RecentActivity = new List<UserActivityLogDto>(), // Would need activity tracking
            Permissions = new List<string>() // Would derive from roles
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
