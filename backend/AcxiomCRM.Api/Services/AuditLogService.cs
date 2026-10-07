using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

// Read-only access to the audit log. Writing is done by AuditService.
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetListAsync(AuditLogQuery query);
    Task<AuditFiltersDto> GetFiltersAsync();
    Task<IQueryable<AuditLog>> BuildQueryAsync(AuditLogQuery query);
}

public class AuditLogService : IAuditLogService
{
    private static readonly string[] KnownActions =
    {
        AuditActions.Login, AuditActions.LoginFailed, AuditActions.Logout, AuditActions.Lockout, AuditActions.Unlock,
        AuditActions.Register, AuditActions.Create, AuditActions.Update, AuditActions.Delete, AuditActions.StatusChange,
        AuditActions.RoleChange, AuditActions.PasswordChange, AuditActions.PasswordReset, AuditActions.Convert,
        AuditActions.Complete, AuditActions.Reschedule,
    };

    private static readonly string[] KnownEntities = { "Auth", "User", "Customer", "Lead", "Opportunity", "FollowUp", "Activity" };

    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;

    public AuditLogService(ApplicationDbContext db, IScopeService scope)
    {
        _db = db;
        _scope = scope;
    }

    // Applies all filters. Managers only see the logs of themselves and their team.
    // Also used by the audit report.
    public async Task<IQueryable<AuditLog>> BuildQueryAsync(AuditLogQuery query)
    {
        var logs = _db.AuditLogs.AsNoTracking();

        var scopeIds = await _scope.GetScopeUserIdsAsync();
        if (scopeIds != null) logs = logs.Where(a => a.UserId != null && scopeIds.Contains(a.UserId.Value));

        if (query.UserId is > 0) logs = logs.Where(a => a.UserId == query.UserId);
        if (!string.IsNullOrEmpty(query.EntityName)) logs = logs.Where(a => a.EntityName == query.EntityName);
        if (!string.IsNullOrEmpty(query.Action)) logs = logs.Where(a => a.Action == query.Action);
        if (!string.IsNullOrEmpty(query.Result)) logs = logs.Where(a => a.Result == query.Result);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            logs = logs.Where(a => EF.Functions.Like(a.Details!, $"%{s}%") || EF.Functions.Like(a.UserName!, $"%{s}%"));
        }

        var (from, to) = QueryFilters.ReadRange(query.DateFrom, query.DateTo);
        var fromUtc = QueryFilters.StartUtc(from);
        var toUtc = QueryFilters.EndUtcExclusive(to);
        if (fromUtc != null) logs = logs.Where(a => a.CreatedDate >= fromUtc);
        if (toUtc != null) logs = logs.Where(a => a.CreatedDate < toUtc);

        return logs;
    }

    public async Task<PagedResult<AuditLogDto>> GetListAsync(AuditLogQuery query)
    {
        var logs = await BuildQueryAsync(query);
        return await logs
            .OrderByDescending(a => a.CreatedDate)
            .ThenByDescending(a => a.AuditLogId)
            .ToPagedResultAsync(query, AuditLogDto.From);
    }

    // Values for the filter drop downs: known values + whatever exists in the table
    public async Task<AuditFiltersDto> GetFiltersAsync()
    {
        var actions = await _db.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();
        var entities = await _db.AuditLogs.Select(a => a.EntityName).Distinct().ToListAsync();

        return new AuditFiltersDto
        {
            Actions = KnownActions.Union(actions).OrderBy(a => a, StringComparer.Ordinal).ToList(),
            Entities = KnownEntities.Union(entities).OrderBy(e => e, StringComparer.Ordinal).ToList(),
        };
    }
}
