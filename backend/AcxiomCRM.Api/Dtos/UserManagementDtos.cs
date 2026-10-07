using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Validation;

namespace AcxiomCRM.Api.Dtos;

public class UserListQuery : PageQuery
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLen(100, "Name")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [ValidEmail]
    [MaxLen(150, "Email")]
    public string? Email { get; set; }

    // Strength rules are checked by the ASP.NET Core Identity password policy
    [Required(ErrorMessage = "Password is required.")]
    [MaxLen(64, "Password")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [OneOf(typeof(Roles))]
    public string? Role { get; set; }

    public int? ManagerId { get; set; }

    public bool? IsActive { get; set; }
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLen(100, "Name")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [ValidEmail]
    [MaxLen(150, "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [OneOf(typeof(Roles))]
    public string? Role { get; set; }

    public int? ManagerId { get; set; }

    public bool? IsActive { get; set; }
}

public class UserStatusRequest
{
    [Required(ErrorMessage = "Active is required.")]
    public bool? IsActive { get; set; }
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "New Password is required.")]
    [MaxLen(64, "New Password")]
    public string? NewPassword { get; set; }
}

public record ManagerLookupDto(int Id, string Name);

public record RoleDto(int Id, string Name, string? Description, int UserCount);

public class PermissionMatrixDto
{
    public List<string> Modules { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Matrix { get; set; } = new();
}
