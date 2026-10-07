using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

// Builds UserDto objects (with role name and manager name) for one or many users
public static class UserMapper
{
    public static async Task<List<UserDto>> ToDtosAsync(ApplicationDbContext db, List<ApplicationUser> users)
    {
        var ids = users.Select(u => u.Id).ToList();
        var roles = await (from ur in db.UserRoles
                           join r in db.Roles on ur.RoleId equals r.Id
                           where ids.Contains(ur.UserId)
                           select new { ur.UserId, r.Id, r.Name }).ToListAsync();
        var managerIds = users.Where(u => u.ManagerId != null).Select(u => u.ManagerId!.Value).Distinct().ToList();
        var managers = await db.Users.Where(u => managerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        return users.Select(u =>
        {
            var role = roles.FirstOrDefault(r => r.UserId == u.Id);
            return new UserDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = role?.Name ?? string.Empty,
                RoleId = role?.Id,
                ManagerId = u.ManagerId,
                ManagerName = u.ManagerId != null && managers.TryGetValue(u.ManagerId.Value, out var m) ? m : null,
                IsActive = u.IsActive,
                IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                LockoutEnd = u.LockoutEnd?.UtcDateTime,
                FailedLoginCount = u.AccessFailedCount,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedDate,
            };
        }).ToList();
    }

    public static async Task<UserDto> ToDtoAsync(ApplicationDbContext db, ApplicationUser user) =>
        (await ToDtosAsync(db, new List<ApplicationUser> { user }))[0];
}
