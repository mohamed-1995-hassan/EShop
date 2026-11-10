using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers.Admin;

/// <summary>
/// Admin User Management
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[Tags("Admin Users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _userService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IAdminUserService userService,
        ILogger<AdminUsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersAsync(
            page, pageSize, search, role, isActive,
            sortBy, sortDescending, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailDto>> GetUser(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminUserDto>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userService.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.DeleteUserAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Update user roles
    /// </summary>
    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateRoles(
        Guid id,
        [FromBody] UserRoleUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userService.UpdateUserRolesAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Lock user account
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockUser(
        Guid id,
        [FromBody] LockUserRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.LockUserAsync(id, request?.LockoutEnd, cancellationToken);
            return Ok(new { message = "User account locked" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Unlock user account
    /// </summary>
    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.UnlockUserAsync(id, cancellationToken);
            return Ok(new { message = "User account unlocked" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Reset user password (sends reset email)
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.ResetPasswordAsync(id, cancellationToken);
            return Ok(new { message = "Password reset email sent" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Terminate all user sessions
    /// </summary>
    [HttpPost("{id:guid}/terminate-sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateSessions(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.TerminateUserSessionsAsync(id, cancellationToken);
            return Ok(new { message = "All user sessions terminated" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// Get available roles
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetAvailableRoles(
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetAvailableRolesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk delete users
    /// </summary>
    [HttpPost("bulk-delete")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkDelete(
        [FromBody] BulkUserIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.BulkDeleteUsersAsync(request.UserIds, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk activate/deactivate users
    /// </summary>
    [HttpPost("bulk-activate")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkActivate(
        [FromBody] BulkActivateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.BulkActivateUsersAsync(request.UserIds, request.Activate, cancellationToken);
        return Ok(result);
    }
}

#region Request DTOs

public class LockUserRequest
{
    public DateTime? LockoutEnd { get; init; }
}

public class BulkUserIdsRequest
{
    public IReadOnlyList<Guid> UserIds { get; init; } = Array.Empty<Guid>();
}

public class BulkActivateRequest
{
    public IReadOnlyList<Guid> UserIds { get; init; } = Array.Empty<Guid>();
    public bool Activate { get; init; }
}

#endregion
