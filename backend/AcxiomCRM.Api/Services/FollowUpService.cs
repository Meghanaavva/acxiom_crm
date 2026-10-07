using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface IFollowUpService
{
    Task<PagedResult<FollowUpDto>> GetListAsync(FollowUpListQuery query);
    Task<FollowUpDto> GetByIdAsync(int id);
    Task<FollowUpDto> CreateAsync(FollowUpRequest request);
    Task<FollowUpDto> UpdateAsync(int id, FollowUpRequest request);
    Task<FollowUpDto> CompleteAsync(int id, string? remarks);
    Task<FollowUpDto> MarkMissedAsync(int id);
    Task<FollowUpDto> CancelAsync(int id);
    Task<FollowUpDto> RescheduleAsync(int id, RescheduleFollowUpRequest request);
    Task DeleteAsync(int id);
}

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public FollowUpService(ApplicationDbContext db, IScopeService scope, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _scope = scope;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<FollowUp> WithRelations() =>
        _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser);

    public async Task<PagedResult<FollowUpDto>> GetListAsync(FollowUpListQuery query)
    {
        var today = DateHelper.Today();
        var followUps = await _scope.ApplyScopeAsync(WithRelations().AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            followUps = followUps.Where(f => EF.Functions.Like(f.Subject, $"%{s}%"));
        }

        if (query.Status == "Overdue")
        {
            followUps = followUps.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate < today);
        }
        else if (!string.IsNullOrEmpty(query.Status))
        {
            followUps = followUps.Where(f => f.Status == query.Status);
        }

        // quick view tabs on the follow-up page
        if (query.View == "today") followUps = followUps.Where(f => f.FollowUpDate == today);
        if (query.View == "upcoming") followUps = followUps.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate >= today);
        if (query.View == "overdue") followUps = followUps.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate < today);

        if (query.AssignedTo is > 0) followUps = followUps.Where(f => f.AssignedTo == query.AssignedTo);
        if (query.CustomerId is > 0) followUps = followUps.Where(f => f.CustomerId == query.CustomerId);
        if (query.LeadId is > 0) followUps = followUps.Where(f => f.LeadId == query.LeadId);
        if (query.OpportunityId is > 0) followUps = followUps.Where(f => f.OpportunityId == query.OpportunityId);

        var (from, to) = QueryFilters.ReadRange(query.DateFrom, query.DateTo);
        if (from != null) followUps = followUps.Where(f => f.FollowUpDate >= from);
        if (to != null) followUps = followUps.Where(f => f.FollowUpDate <= to);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<FollowUp, object?>>>
        {
            ["followUpDate"] = f => f.FollowUpDate,
            ["subject"] = f => f.Subject,
            ["status"] = f => f.Status,
            ["createdAt"] = f => f.CreatedDate,
        };

        // default: nearest follow-up first
        return await followUps.SortBy(query, sortColumns, "followUpDate", defaultAscending: true)
            .ToPagedResultAsync(query, FollowUpDto.From);
    }

    // Loads a follow-up and checks that the user is allowed to see it
    private async Task<FollowUp> GetEntityAsync(int id)
    {
        var followUp = await WithRelations().FirstOrDefaultAsync(f => f.FollowUpId == id)
            ?? throw AppException.NotFound("Follow-up");
        await _scope.EnsureInScopeAsync(followUp.AssignedTo);
        return followUp;
    }

    public async Task<FollowUpDto> GetByIdAsync(int id) => FollowUpDto.From(await GetEntityAsync(id));

    public async Task<FollowUpDto> CreateAsync(FollowUpRequest request)
    {
        var user = await _currentUser.GetAsync();
        var date = request.FollowUpDate!.Value;
        CheckFollowUpDate(date);
        await ValidateRelatedRecordsAsync(request);
        var assignedTo = await _scope.ValidateAssigneeAsync(request.AssignedTo);

        var followUp = new FollowUp
        {
            Status = FollowUpStatuses.Planned,
            AssignedTo = assignedTo,
            CreatedBy = user.Id,
        };
        Apply(followUp, request);

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create,
            EntityName = "FollowUp",
            RecordId = followUp.FollowUpId,
            NewValue = Snapshot(followUp),
        });
        return await GetByIdAsync(followUp.FollowUpId);
    }

    public async Task<FollowUpDto> UpdateAsync(int id, FollowUpRequest request)
    {
        var followUp = await GetEntityAsync(id);
        if (followUp.Status != FollowUpStatuses.Planned)
        {
            throw AppException.Conflict($"A {followUp.Status.ToLower()} follow-up cannot be edited.");
        }

        CheckFollowUpDate(request.FollowUpDate!.Value);
        await ValidateRelatedRecordsAsync(request);
        var assignedTo = request.AssignedTo is > 0 ? await _scope.ValidateAssigneeAsync(request.AssignedTo) : followUp.AssignedTo;

        var before = Snapshot(followUp);
        Apply(followUp, request);
        followUp.AssignedTo = assignedTo;
        await _db.SaveChangesAsync();

        var (oldValues, newValues) = AuditService.Diff(before, Snapshot(followUp));
        if (newValues.Count > 0)
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.Update,
                EntityName = "FollowUp",
                RecordId = id,
                OldValue = oldValues,
                NewValue = newValues,
            });
        }
        return await GetByIdAsync(id);
    }

    public async Task<FollowUpDto> CompleteAsync(int id, string? remarks)
    {
        var followUp = await GetEntityAsync(id);
        if (followUp.Status != FollowUpStatuses.Planned && followUp.Status != FollowUpStatuses.Missed)
        {
            throw AppException.Conflict("Only planned or missed follow-ups can be completed.");
        }

        var oldStatus = followUp.Status;
        followUp.Status = FollowUpStatuses.Completed;
        followUp.CompletedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(remarks)) followUp.Remarks = remarks.Trim();
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Complete,
            EntityName = "FollowUp",
            RecordId = id,
            OldValue = new { status = oldStatus },
            NewValue = new { status = FollowUpStatuses.Completed, remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim() },
        });

        // Completing a follow-up on a new lead means the lead has now been contacted
        if (followUp.LeadId != null)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.LeadId == followUp.LeadId);
            if (lead != null && lead.Status == LeadStatuses.New)
            {
                lead.Status = LeadStatuses.Contacted;
                await _db.SaveChangesAsync();
                await _audit.LogAsync(new AuditEntry
                {
                    Action = AuditActions.StatusChange,
                    EntityName = "Lead",
                    RecordId = lead.LeadId,
                    OldValue = new { status = LeadStatuses.New },
                    NewValue = new { status = LeadStatuses.Contacted },
                    Details = $"Status changed from New to Contacted after completing follow-up #{id}",
                });
            }
        }
        return await GetByIdAsync(id);
    }

    public Task<FollowUpDto> MarkMissedAsync(int id) =>
        ChangeStatusAsync(id, FollowUpStatuses.Missed, FollowUpStatuses.Planned);

    public Task<FollowUpDto> CancelAsync(int id) =>
        ChangeStatusAsync(id, FollowUpStatuses.Cancelled, FollowUpStatuses.Planned, FollowUpStatuses.Missed);

    private async Task<FollowUpDto> ChangeStatusAsync(int id, string newStatus, params string[] allowedFrom)
    {
        var followUp = await GetEntityAsync(id);
        if (!allowedFrom.Contains(followUp.Status))
        {
            throw AppException.Conflict($"A {followUp.Status.ToLower()} follow-up cannot be marked as {newStatus.ToLower()}.");
        }

        var oldStatus = followUp.Status;
        followUp.Status = newStatus;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.StatusChange,
            EntityName = "FollowUp",
            RecordId = id,
            OldValue = new { status = oldStatus },
            NewValue = new { status = newStatus },
            Details = $"Status changed from {oldStatus} to {newStatus}",
        });
        return await GetByIdAsync(id);
    }

    public async Task<FollowUpDto> RescheduleAsync(int id, RescheduleFollowUpRequest request)
    {
        var followUp = await GetEntityAsync(id);
        if (followUp.Status != FollowUpStatuses.Planned && followUp.Status != FollowUpStatuses.Missed)
        {
            throw AppException.Conflict($"A {followUp.Status.ToLower()} follow-up cannot be rescheduled.");
        }

        var newDate = request.FollowUpDate!.Value;
        CheckFollowUpDate(newDate);

        var oldDate = followUp.FollowUpDate;
        var oldStatus = followUp.Status;
        followUp.FollowUpDate = newDate;
        followUp.Status = FollowUpStatuses.Planned;
        if (!string.IsNullOrWhiteSpace(request.Remarks)) followUp.Remarks = request.Remarks.Trim();
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Reschedule,
            EntityName = "FollowUp",
            RecordId = id,
            OldValue = new { followUpDate = oldDate.ToString("yyyy-MM-dd"), status = oldStatus },
            NewValue = new { followUpDate = newDate.ToString("yyyy-MM-dd"), status = FollowUpStatuses.Planned },
            Details = $"Rescheduled from {oldDate:yyyy-MM-dd} to {newDate:yyyy-MM-dd}",
        });
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var followUp = await GetEntityAsync(id);
        var snapshot = FollowUpDto.From(followUp);

        _db.FollowUps.Remove(followUp); // soft delete (see ApplicationDbContext)
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Delete,
            EntityName = "FollowUp",
            RecordId = id,
            OldValue = snapshot,
        });
    }

    // Business rule: a new / planned follow-up cannot be scheduled in the past
    private static void CheckFollowUpDate(DateOnly date)
    {
        if (date < DateHelper.Today())
        {
            throw AppException.Field("followUpDate", "Follow-up date cannot be earlier than today.");
        }
    }

    // The follow-up must be linked to at least one record, and linked records must be visible to the user
    private async Task ValidateRelatedRecordsAsync(FollowUpRequest request)
    {
        if (request.CustomerId is not > 0 && request.LeadId is not > 0 && request.OpportunityId is not > 0)
        {
            throw AppException.Field("customerId", "Select a related customer, lead or opportunity.");
        }

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

        if (request.OpportunityId is > 0)
        {
            var opportunity = await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.OpportunityId == request.OpportunityId)
                ?? throw AppException.Field("opportunityId", "Selected opportunity does not exist.");
            await _scope.EnsureInScopeAsync(opportunity.AssignedTo);
        }
    }

    private static void Apply(FollowUp f, FollowUpRequest r)
    {
        f.Subject = r.Subject!.Trim();
        f.FollowUpType = r.FollowUpType!;
        f.FollowUpDate = r.FollowUpDate!.Value;
        f.Remarks = string.IsNullOrWhiteSpace(r.Remarks) ? null : r.Remarks.Trim();
        f.CustomerId = r.CustomerId is > 0 ? r.CustomerId : null;
        f.LeadId = r.LeadId is > 0 ? r.LeadId : null;
        f.OpportunityId = r.OpportunityId is > 0 ? r.OpportunityId : null;
    }

    private static Dictionary<string, object?> Snapshot(FollowUp f) => new()
    {
        ["subject"] = f.Subject,
        ["followUpType"] = f.FollowUpType,
        ["followUpDate"] = f.FollowUpDate.ToString("yyyy-MM-dd"),
        ["status"] = f.Status,
        ["remarks"] = f.Remarks,
        ["customerId"] = f.CustomerId,
        ["leadId"] = f.LeadId,
        ["opportunityId"] = f.OpportunityId,
        ["assignedTo"] = f.AssignedTo,
    };
}
