using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Api.Services;

public interface IAuthService
{
    Task<UserDto> LoginAsync(LoginRequest request);
    Task RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<UserDto> GetCurrentUserAsync();
    Task ChangePasswordAsync(ChangePasswordRequest request);
}

/// <summary>
/// Authentication using ASP.NET Core Identity:
///  - UserManager stores only Identity password hashes (PBKDF2, never plain text)
///  - Password policy and lockout are configured in Program.cs (IdentityOptions)
///  - SignInManager issues the secure, HttpOnly authentication cookie
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly IPasswordHistoryService _passwordHistory;

    public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit, IPasswordHistoryService passwordHistory)
    {
        _passwordHistory = passwordHistory;
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<UserDto> LoginAsync(LoginRequest request)
    {
        var email = request.Email!.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            await LogFailedLogin(null, email, "Unknown email");
            throw AppException.Unauthorized("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            await LogFailedLogin(user, email, "Account is inactive");
            throw AppException.Forbidden("Your account is inactive. Please contact the administrator.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            await LogFailedLogin(user, email, "Account is locked");
            throw AppException.Forbidden(LockedMessage(user));
        }

        // lockoutOnFailure: true -> Identity counts failed attempts and locks the account
        var result = await _signInManager.PasswordSignInAsync(user, request.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.Lockout,
                EntityName = "Auth",
                RecordId = user.Id,
                UserId = user.Id,
                UserName = user.FullName,
                Result = "Failure",
                Details = "Account locked after too many failed login attempts",
            });
            throw AppException.Forbidden(LockedMessage(user));
        }

        if (!result.Succeeded)
        {
            await LogFailedLogin(user, email, $"Wrong password (attempt {user.AccessFailedCount})");
            throw AppException.Unauthorized("Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Login,
            EntityName = "Auth",
            RecordId = user.Id,
            UserId = user.Id,
            UserName = user.FullName,
            Details = "User logged in",
        });
        return await UserMapper.ToDtoAsync(_db, user);
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            FullName = request.Name!.Trim(),
            Email = request.Email!.Trim().ToLower(),
            UserName = request.Email!.Trim().ToLower(),
            IsActive = true,
            LockoutEnabled = true,
        };

        // Identity validates the password policy and hashes the password
        var result = await _userManager.CreateAsync(user, request.Password!);
        if (!result.Succeeded) throw IdentityErrorHelper.ToException(result);

        // Self registered users always get the lowest role
        await _userManager.AddToRoleAsync(user, Roles.SalesExecutive);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Register,
            EntityName = "Auth",
            RecordId = user.Id,
            UserId = user.Id,
            UserName = user.FullName,
            NewValue = new { name = user.FullName, email = user.Email, role = Roles.SalesExecutive },
        });
    }

    public async Task LogoutAsync()
    {
        var current = await _currentUser.GetAsync();
        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Logout,
            EntityName = "Auth",
            RecordId = current.Id,
            Details = "User logged out",
        });

        var user = await _userManager.FindByIdAsync(current.Id.ToString());
        if (user != null)
        {
            // new security stamp = every existing cookie of this user stops working
            await _userManager.UpdateSecurityStampAsync(user);
        }
        await _signInManager.SignOutAsync();
    }

    public async Task<UserDto> GetCurrentUserAsync()
    {
        var current = await _currentUser.GetAsync();
        var user = await _userManager.FindByIdAsync(current.Id.ToString()) ?? throw AppException.Unauthorized();
        return await UserMapper.ToDtoAsync(_db, user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var current = await _currentUser.GetAsync();
        var user = await _userManager.FindByIdAsync(current.Id.ToString()) ?? throw AppException.Unauthorized();

        if (request.CurrentPassword == request.NewPassword)
        {
            throw AppException.Field("newPassword", "New password must be different from the current password.");
        }

        // Current password must be right before we tell anything about the new one
        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword!))
        {
            throw AppException.Field("currentPassword", "Current password is incorrect.");
        }

        // Password history: the last 5 passwords cannot be used again
        await _passwordHistory.EnsureNotReusedAsync(user, request.NewPassword!, "newPassword");
        var oldHash = user.PasswordHash;

        // ChangePasswordAsync checks the current password, applies the policy and updates the security stamp
        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword!, request.NewPassword!);
        if (!result.Succeeded)
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.PasswordChange,
                EntityName = "Auth",
                RecordId = user.Id,
                Result = "Failure",
                Details = "Password change failed",
            });
            throw IdentityErrorHelper.ToException(result, "newPassword");
        }

        await _passwordHistory.SaveAsync(user.Id, oldHash);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.PasswordChange,
            EntityName = "Auth",
            RecordId = user.Id,
            Details = "User changed password",
        });
        await _signInManager.SignOutAsync();
    }

    private async Task LogFailedLogin(ApplicationUser? user, string email, string reason)
    {
        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.LoginFailed,
            EntityName = "Auth",
            RecordId = user?.Id,
            UserId = user?.Id,
            UserName = user?.FullName ?? email,
            Result = "Failure",
            Details = reason,
        });
    }

    private static string LockedMessage(ApplicationUser user)
    {
        var minutes = user.LockoutEnd.HasValue
            ? Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes))
            : 1;
        return $"Account is locked due to too many failed login attempts. Try again after {minutes} minute(s).";
    }
}
