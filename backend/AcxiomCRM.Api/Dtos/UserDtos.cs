using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Validation;

namespace AcxiomCRM.Api.Dtos;

// Returned to the client. Never contains PasswordHash, SecurityStamp etc.
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record UserLookupDto(int Id, string Name, string Role);

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [ValidEmail]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    public string? Password { get; set; }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLen(100, "Name")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [ValidEmail]
    [MaxLen(150, "Email")]
    public string? Email { get; set; }

    // Strength rules (length, upper, lower, digit, special) are checked by ASP.NET Core Identity password policy
    [Required(ErrorMessage = "Password is required.")]
    [MaxLen(64, "Password")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current Password is required.")]
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "New Password is required.")]
    [MaxLen(64, "New Password")]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}

public class LoginResponse
{
    public UserDto User { get; set; } = new();
}
