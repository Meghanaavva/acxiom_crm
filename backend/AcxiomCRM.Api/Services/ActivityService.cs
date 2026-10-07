using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface IActivityService
{
    Task<PagedResult<ActivityDto>> GetListAsync(ActivityListQuery query);
    Task<ActivityDto> GetByIdAsync(int id);
    Task<ActivityDto> CreateAsync(ActivityRequest request);
    Task<ActivityDto> UpdateAsync(int id, ActivityRequest request);
    Task DeleteAsync(int id);
}

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public ActivityService(ApplicationDbContext db, IScopeService scope, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _scope = scope;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<Activity> WithRelations() =>
        _db.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser);

    public async Task<PagedResult<ActivityDto>> GetListAsync(ActivityListQuery query)
    {
        var activities = await _scope.ApplyScopeAsync(WithRelations().AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            activities = activities.Where(a => EF.Functions.Like(a.Subject, $"%{s}%"));
        }
        if (!string.IsNullOrEmpty(query.ActivityType)) activities = activities.Where(a => a.ActivityType == query.ActivityType);
        if (!string.IsNullOrEmpty(query.Status)) activities = activities.Where(a => a.Status == query.Status);
        if (query.AssignedTo is > 0) activities = activities.Where(a => a.AssignedTo == query.AssignedTo);
        if (query.CustomerId is > 0) activities = activities.Where(a => a.CustomerId == query.CustomerId);
        if (query.LeadId is > 0) activities = activities.Where(a => a.LeadId == query.LeadId);

        var (from, to) = QueryFilters.ReadRange(query.DateFrom, query.DateTo);
        var fromUtc = QueryFilters.StartUtc(from);
        var toUtc = QueryFilters.EndUtcExclusive(to);
        if (fromUtc != null) activities = activities.Where(a => a.ActivityDate >= fromUtc);
        if (toUtc != null) activities = activities.Where(a => a.ActivityDate < toUtc);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<Activity, object?>>>
        {
            ["activityDate"] = a => a.ActivityDate,
            ["subject"] = a => a.Subject,
            ["activityType"] = a => a.ActivityType,
            ["status"] = a => a.Status,
            ["createdAt"] = a => a.CreatedDate,
        };

        return await activities.SortBy(query, sortColumns, "activityDate").ToPagedResultAsync(query, ActivityDto.From);
    }

    // Loads an activity and checks that the user is allowed to see it
    private async Task<Activity> GetEntityAsync(int id)
    {
        var activity = await WithRelations().FirstOrDefaultAsync(a => a.ActivityId == id)
            ?? throw AppException.NotFound("Activity");
        await _scope.EnsureInScopeAsync(activity.AssignedTo);
        return activity;
    }

    public async Task<ActivityDto> GetByIdAsync(int id) => ActivityDto.From(await GetEntityAsync(id));

    public async Task<ActivityDto> CreateAsync(ActivityRequest request)
    {
        var user = await _currentUser.GetAsync();
        await ValidateRelatedRecordsAsync(request);
        var assignedTo = await _scope.ValidateAssigneeAsync(request.AssignedTo);

        var activity = new Activity { AssignedTo = assignedTo, CreatedBy = user.Id };
        Apply(activity, request);

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create,
            EntityName = "Activity",
            RecordId = activity.ActivityId,
            NewValue = Snapshot(activity),
        });
        return await GetByIdAsync(activity.ActivityId);
    }

    public async Task<ActivityDto> UpdateAsync(int id, ActivityRequest request)
    {
        var activity = await GetEntityAsync(id);
        await ValidateRelatedRecordsAsync(request);
        var assignedTo = request.AssignedTo is > 0 ? await _scope.ValidateAssigneeAsync(request.AssignedTo) : activity.AssignedTo;

        var before = Snapshot(activity);
        Apply(activity, request);
        activity.AssignedTo = assignedTo;
        await _db.SaveChangesAsync();

        var (oldValues, newValues) = AuditService.Diff(before, Snapshot(activity));
        if (newValues.Count > 0)
        {
            var statusChanged = newValues.ContainsKey("status");
            await _audit.LogAsync(new AuditEntry
            {
                Action = statusChanged ? AuditActions.StatusChange : AuditActions.Update,
                EntityName = "Activity",
                RecordId = id,
                OldValue = oldValues,
                NewValue = newValues,
                Details = statusChanged ? $"Status changed from {oldValues["status"]} to {newValues["status"]}" : null,
            });
        }
        return await GetByIdAsync(id);
    }

    // The scope check already limits sales executives to their own activities
    public async Task DeleteAsync(int id)
    {
        var activity = await GetEntityAsync(id);
        var snapshot = ActivityDto.From(activity);

        _db.Activities.Remove(activity); // soft delete (see ApplicationDbContext)
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Delete,
            EntityName = "Activity",
            RecordId = id,
            OldValue = snapshot,
        });
    }

    // Linked customer / lead are optional, but if given they must exist and be visible to the user
    private async Task ValidateRelatedRecordsAsync(ActivityRequest request)
    {
        if (request.CustomerId is > 0)
        {
            var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId)
                ?? throw AppException.Field("customerId", "Selected customer does not exist.");
            await _scope.EnsureInScopeAsync(customer.AssignedTo);
        }

        if (request.LeadId is > 0)
        {
            var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == request.LeadId)
                ?? throw AppException.Field("leadId", "Selected lead does not exist.");
            await _scope.EnsureInScopeAsync(lead.AssignedTo);
        }
    }

    private static void Apply(Activity a, ActivityRequest r)
    {
        a.ActivityType = r.ActivityType!;
        a.Subject = r.Subject!.Trim();
        a.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        a.ActivityDate = ToUtc(r.ActivityDate!.Value);
        a.Status = r.Status!;
        a.CustomerId = r.CustomerId is > 0 ? r.CustomerId : null;
        a.LeadId = r.LeadId is > 0 ? r.LeadId : null;
    }

    // The client sends ISO dates in UTC ("...Z"). Dates without a zone are treated as UTC too.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static Dictionary<string, object?> Snapshot(Activity a) => new()
    {
        ["activityType"] = a.ActivityType,
        ["subject"] = a.Subject,
        ["description"] = a.Description,
        // ISO string so an unchanged date is not reported as a change
        ["activityDate"] = a.ActivityDate.ToString("o"),
        ["status"] = a.Status,
        ["customerId"] = a.CustomerId,
        ["leadId"] = a.LeadId,
        ["assignedTo"] = a.AssignedTo,
    };
}
