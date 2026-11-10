using EShop.Application.DTOs;

namespace EShop.Application.Interfaces.Admin;

/// <summary>
/// Service interface for admin user management operations
/// </summary>
public interface IAdminUserService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(
        int page = 1, 
        int pageSize = 20,
        string? search = null,
        string? role = null,
        bool? isActive = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default);
    
    Task<AdminUserDetailDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task<AdminUserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    
    Task<AdminUserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task<AdminUserDto> UpdateUserRolesAsync(Guid userId, UserRoleUpdateRequest request, CancellationToken cancellationToken = default);
    
    Task LockUserAsync(Guid userId, DateTime? lockoutEnd = null, CancellationToken cancellationToken = default);
    
    Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task TerminateUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkDeleteUsersAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken = default);
    
    Task<BulkOperationResultDto> BulkActivateUsersAsync(IReadOnlyList<Guid> userIds, bool activate, CancellationToken cancellationToken = default);
}
