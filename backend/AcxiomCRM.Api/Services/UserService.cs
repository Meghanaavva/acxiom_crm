using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetListAsync(UserListQuery query);
    Task<List<UserLookupDto>> GetLookupAsync();
    Task<List<ManagerLookupDto>> GetManagersAsync();
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request);
    Task<UserDto> SetStatusAsync(int id, bool isActive);
    Task ResetPasswordAsync(int id, string newPassword);
    Task<UserDto> UnlockAsync(int id);
    Task<List<RoleDto>> GetRolesAsync();
}

/// <summary>
/// User administration. All password work (hashing, policy, reset) goes through ASP.NET Core Identity UserManager.
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IScopeService _scope;
    private readonly IAuditService _audit;
    private readonly IPasswordHistoryService _passwordHistory;

    public UserService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ICurrentUserService currentUser,
        IScopeService scope, IAuditService audit, IPasswordHistoryService passwordHistory)
    {
        _passwordHistory = passwordHistory;
        _db = db;
        _userManager = userManager;
        _currentUser = currentUser;
        _scope = scope;
        _audit = audit;
    }

    public async Task<PagedResult<UserDto>> GetListAsync(UserListQuery query)
    {
        var users = _db.Users.AsNoTracking().AsQueryable();

        // Manager can only see himself and his team
        var scopeIds = await _scope.GetScopeUserIdsAsync();
        if (scopeIds != null) users = users.Where(u => scopeIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            users = users.Where(u => EF.Functions.Like(u.FullName, $"%{s}%") || EF.Functions.Like(u.Email!, $"%{s}%"));
        }
        if (query.IsActive.HasValue) users = users.Where(u => u.IsActive == query.IsActive.Value);
        if (!string.IsNullOrEmpty(query.Role))
        {
            var userIdsInRole = from ur in _db.UserRoles
                                join r in _db.Roles on ur.RoleId equals r.Id
                                where r.Name == query.Role
                                select ur.UserId;
            users = users.Where(u => userIdsInRole.Contains(u.Id));
        }

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<ApplicationUser, object?>>>
        {
            ["name"] = u => u.FullName,
            ["email"] = u => u.Email,
            ["isActive"] = u => u.IsActive,
            ["lastLoginAt"] = u => u.LastLoginAt,
            ["createdAt"] = u => u.CreatedDate,
        };
        users = users.SortBy(query, sortColumns, "createdAt");

        // UserDto needs role names, so paging is done here instead of ToPagedResultAsync
        var total = await users.CountAsync();
        var page = await users.Skip(query.Skip).Take(query.PageSize).ToListAsync();
        return new PagedResult<UserDto>
        {
            Data = await UserMapper.ToDtosAsync(_db, page),
            Pagination = new PaginationInfo
            {
                Page = query.Page,
                PageSize = query.PageSize,
                Total = total,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)query.PageSize)),
            },
        };
    }

    // Users the caller can assign records to (Admin: everyone, Manager: self + team, Sales: self)
    public async Task<List<UserLookupDto>> GetLookupAsync()
    {
        var scopeIds = await _scope.GetScopeUserIdsAsync();
        var users = _db.Users.AsNoTracking().Where(u => u.IsActive);
        if (scopeIds != null) users = users.Where(u => scopeIds.Contains(u.Id));

        var list = await (from u in users
                          join ur in _db.UserRoles on u.Id equals ur.UserId into userRoles
                          from ur in userRoles.DefaultIfEmpty()
                          join r in _db.Roles on ur.RoleId equals r.Id into roles
                          from r in roles.DefaultIfEmpty()
                          orderby u.FullName
                          select new { u.Id, u.FullName, RoleName = r != null ? r.Name : null }).ToListAsync();

        return list.Select(u => new UserLookupDto(u.Id, u.FullName, u.RoleName ?? string.Empty)).ToList();
    }

    public async Task<List<ManagerLookupDto>> GetManagersAsync()
    {
        var managers = await _userManager.GetUsersInRoleAsync(Roles.Manager);
        return managers
            .Where(m => m.IsActive)
            .OrderBy(m => m.FullName)
            .Select(m => new ManagerLookupDto(m.Id, m.FullName))
            .ToList();
    }

    private async Task<ApplicationUser> GetEntityAsync(int id)
    {
        return await _userManager.FindByIdAsync(id.ToString()) ?? throw AppException.NotFound("User");
    }

    public async Task<UserDto> GetByIdAsync(int id) => await UserMapper.ToDtoAsync(_db, await GetEntityAsync(id));

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email!.Trim().ToLower();
        await CheckEmailUniqueAsync(email, null);
        var managerId = await ResolveManagerIdAsync(request.Role!, request.ManagerId);

        var user = new ApplicationUser
        {
            FullName = request.Name!.Trim(),
            Email = email,
            UserName = email,
            ManagerId = managerId,
            IsActive = request.IsActive ?? true,
            LockoutEnabled = true,
            CreatedDate = DateTime.UtcNow,
        };

        // Identity checks the password policy and stores only the password hash
        var result = await _userManager.CreateAsync(user, request.Password!);
        if (!result.Succeeded) throw IdentityErrorHelper.ToException(result);

        await _userManager.AddToRoleAsync(user, request.Role!);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create,
            EntityName = "User",
            RecordId = user.Id,
            NewValue = new { name = user.FullName, email, role = request.Role, managerId, isActive = user.IsActive },
            Details = $"User {user.FullName} created",
        });
        return await GetByIdAsync(user.Id);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request)
    {
        var current = await _currentUser.GetAsync();
        var user = await GetEntityAsync(id);
        var isSelf = current.Id == id;
        var email = request.Email!.Trim().ToLower();
        var newRole = request.Role!;
        var isActive = request.IsActive ?? user.IsActive;

        await CheckEmailUniqueAsync(email, id);

        if (isSelf && newRole != Roles.Admin) throw AppException.BadRequest("You cannot remove your own Admin role.");
        if (isSelf && !isActive) throw AppException.BadRequest("You cannot deactivate your own account.");

        var managerId = await ResolveManagerIdAsync(newRole, request.ManagerId);
        var oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;

        // A manager that still has team members can't be moved to another role
        if (oldRole == Roles.Manager && newRole != Roles.Manager)
        {
            var hasTeam = await _db.Users.AnyAsync(u => u.ManagerId == id);
            if (hasTeam)
            {
                throw AppException.Conflict("This manager still has team members. Move them to another manager first.");
            }
        }

        var before = new Dictionary<string, object?>
        {
            ["name"] = user.FullName,
            ["email"] = user.Email,
            ["role"] = oldRole,
            ["managerId"] = user.ManagerId,
            ["isActive"] = user.IsActive,
        };
        var after = new Dictionary<string, object?>
        {
            ["name"] = request.Name!.Trim(),
            ["email"] = email,
            ["role"] = newRole,
            ["managerId"] = managerId,
            ["isActive"] = isActive,
        };
        var (oldValues, newValues) = AuditService.Diff(before, after);

        user.FullName = request.Name!.Trim();
        user.Email = email;
        user.UserName = email;
        user.ManagerId = managerId;
        user.IsActive = isActive;

        // UpdateAsync also updates the normalized email / user name
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) throw IdentityErrorHelper.ToException(result);

        var roleChanged = oldRole != newRole;
        if (roleChanged)
        {
            if (!string.IsNullOrEmpty(oldRole)) await _userManager.RemoveFromRoleAsync(user, oldRole);
            await _userManager.AddToRoleAsync(user, newRole);
        }

        // Role or status change -> existing login cookie of this user stops working
        if (roleChanged || before["isActive"] is bool wasActive && wasActive != isActive)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        if (newValues.Count > 0)
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.Update,
                EntityName = "User",
                RecordId = id,
                OldValue = oldValues,
                NewValue = newValues,
            });
        }
        if (roleChanged)
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.RoleChange,
                EntityName = "User",
                RecordId = id,
                OldValue = new { role = oldRole },
                NewValue = new { role = newRole },
                Details = $"Role of {user.FullName} changed from {oldRole} to {newRole}",
            });
        }
        return await GetByIdAsync(id);
    }

    public async Task<UserDto> SetStatusAsync(int id, bool isActive)
    {
        var current = await _currentUser.GetAsync();
        var user = await GetEntityAsync(id);
        if (current.Id == id && !isActive) throw AppException.BadRequest("You cannot deactivate your own account.");

        if (user.IsActive != isActive)
        {
            user.IsActive = isActive;
            await _userManager.UpdateAsync(user);
            if (!isActive)
            {
                // log the user out everywhere
                await _userManager.UpdateSecurityStampAsync(user);
            }

            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.StatusChange,
                EntityName = "User",
                RecordId = id,
                OldValue = new { isActive = !isActive },
                NewValue = new { isActive },
                Details = $"{user.FullName} was {(isActive ? "activated" : "deactivated")}",
            });
        }
        return await GetByIdAsync(id);
    }

    public async Task ResetPasswordAsync(int id, string newPassword)
    {
        var user = await GetEntityAsync(id);

        // Recent passwords of this user cannot be set again
        await _passwordHistory.EnsureNotReusedAsync(user, newPassword, "newPassword");
        var oldHash = user.PasswordHash;

        // Admin reset: Identity reset token + new password (policy is applied, stamp is updated)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) throw IdentityErrorHelper.ToException(result, "newPassword");
        await _passwordHistory.SaveAsync(user.Id, oldHash);

        // Reset also unlocks the account
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.PasswordReset,
            EntityName = "User",
            RecordId = id,
            Details = $"Password reset by administrator for {user.FullName}",
        });
    }

    public async Task<UserDto> UnlockAsync(int id)
    {
        var user = await GetEntityAsync(id);
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Unlock,
            EntityName = "User",
            RecordId = id,
            Details = $"Account of {user.FullName} unlocked by administrator",
        });
        return await GetByIdAsync(id);
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
        var counts = await _db.UserRoles.GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count);

        return roles
            .Select(r => new RoleDto(r.Id, r.Name ?? string.Empty, r.Description, counts.GetValueOrDefault(r.Id)))
            .ToList();
    }

    private async Task CheckEmailUniqueAsync(string email, int? excludeId)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null && existing.Id != excludeId)
        {
            throw AppException.Conflict("Duplicate user.",
                new List<FieldError> { new("email", "A user with this email already exists.") });
        }
    }

    // Manager is only used for Sales Executives and must be an active Manager
    private async Task<int?> ResolveManagerIdAsync(string role, int? managerId)
    {
        if (role != Roles.SalesExecutive || managerId is null or <= 0) return null;

        var manager = await _userManager.FindByIdAsync(managerId.Value.ToString());
        if (manager == null || !manager.IsActive || !await _userManager.IsInRoleAsync(manager, Roles.Manager))
        {
            throw AppException.Field("managerId", "Selected manager does not exist or is inactive.");
        }
        return manager.Id;
    }
}
