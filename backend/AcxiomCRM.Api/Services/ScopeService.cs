using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

/// <summary>
/// Row level security for CRM records.
///  - Admin          : all records
///  - Manager        : his own records + records of his team members
///  - SalesExecutive : only his own records
/// </summary>
public interface IScopeService
{
    Task<List<int>?> GetScopeUserIdsAsync();
    Task<IQueryable<T>> ApplyScopeAsync<T>(IQueryable<T> query) where T : class, IAssignable;
    Task EnsureInScopeAsync(int assignedTo);
    Task<int> ValidateAssigneeAsync(int? assignedTo, string field = "assignedTo");
}

public class ScopeService : IScopeService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private List<int>? _scopeIds;
    private bool _loaded;

    public ScopeService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // null means "no restriction" (Admin)
    public async Task<List<int>?> GetScopeUserIdsAsync()
    {
        if (_loaded) return _scopeIds;
        var user = await _currentUser.GetAsync();

        if (user.IsAdmin)
        {
            _scopeIds = null;
        }
        else if (user.IsManager)
        {
            var team = await _db.Users.Where(u => u.ManagerId == user.Id).Select(u => u.Id).ToListAsync();
            _scopeIds = new List<int> { user.Id };
            _scopeIds.AddRange(team);
        }
        else
        {
            _scopeIds = new List<int> { user.Id };
        }

        _loaded = true;
        return _scopeIds;
    }

    public async Task<IQueryable<T>> ApplyScopeAsync<T>(IQueryable<T> query) where T : class, IAssignable
    {
        var ids = await GetScopeUserIdsAsync();
        return ids == null ? query : query.Where(x => ids.Contains(x.AssignedTo));
    }

    // Throws 403 when the record belongs to someone outside the user's scope
    public async Task EnsureInScopeAsync(int assignedTo)
    {
        var ids = await GetScopeUserIdsAsync();
        if (ids != null && !ids.Contains(assignedTo))
        {
            throw AppException.Forbidden("You do not have permission to access this record.");
        }
    }

    // The assigned user must be active and inside the caller's scope. Defaults to the caller.
    public async Task<int> ValidateAssigneeAsync(int? assignedTo, string field = "assignedTo")
    {
        var user = await _currentUser.GetAsync();
        var targetId = assignedTo is > 0 ? assignedTo.Value : user.Id;

        var ids = await GetScopeUserIdsAsync();
        if (ids != null && !ids.Contains(targetId))
        {
            throw AppException.Field(field, "You cannot assign records to this user.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Id == targetId && u.IsActive);
        if (!exists)
        {
            throw AppException.Field(field, "Assigned user does not exist or is inactive.");
        }
        return targetId;
    }
}
