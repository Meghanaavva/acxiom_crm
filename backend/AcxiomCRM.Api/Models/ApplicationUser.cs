using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Api.Models;

// Extends the ASP.NET Core Identity user.
// Identity already gives us: Email, UserName, PasswordHash, AccessFailedCount (FailedLoginCount),
// LockoutEnd, LockoutEnabled, SecurityStamp etc. We only add CRM specific fields.
public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Sales executives report to a manager. Used for "team" data scope.
    public int? ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
