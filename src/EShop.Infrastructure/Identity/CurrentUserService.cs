using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace EShop.Infrastructure.Identity;

/// <summary>
/// Service to access current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    Guid? CustomerProfileId { get; }
    Guid? VendorId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsVendor { get; }
    bool IsCustomer { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            
            return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) 
                ? userId 
                : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value 
        ?? User?.FindFirst("email")?.Value;

    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? new List<string>();

    public Guid? CustomerProfileId
    {
        get
        {
            var claim = User?.FindFirst("customer_profile_id")?.Value;
            return claim != null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public Guid? VendorId
    {
        get
        {
            var claim = User?.FindFirst("vendor_id")?.Value;
            return claim != null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => Roles.Contains("Admin");

    public bool IsVendor => Roles.Contains("Vendor");

    public bool IsCustomer => Roles.Contains("Customer");
}

/// <summary>
/// Extension methods for ClaimsPrincipal
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? principal.FindFirst("sub")?.Value;
        
        return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) 
            ? userId 
            : null;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value 
            ?? principal.FindFirst("email")?.Value;
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    public static Guid? GetCustomerProfileId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("customer_profile_id")?.Value;
        return claim != null && Guid.TryParse(claim, out var id) ? id : null;
    }

    public static Guid? GetVendorId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("vendor_id")?.Value;
        return claim != null && Guid.TryParse(claim, out var id) ? id : null;
    }

    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role);
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin");
    }

    public static bool IsVendor(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Vendor");
    }

    public static bool IsCustomer(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Customer");
    }
}
