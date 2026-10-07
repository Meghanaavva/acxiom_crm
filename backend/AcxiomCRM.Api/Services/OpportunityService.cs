using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface IOpportunityService
{
    Task<PagedResult<OpportunityDto>> GetListAsync(OpportunityListQuery query);
    Task<PipelineDto> GetPipelineAsync();
    Task<OpportunityDto> GetByIdAsync(int id);
    Task<OpportunityDto> CreateAsync(OpportunityRequest request);
    Task<OpportunityDto> UpdateAsync(int id, OpportunityRequest request);
    Task<OpportunityDto> ChangeStageAsync(int id, string stage);
    Task DeleteAsync(int id);

    // Business rules from section 5.3 - also used by lead conversion
    void ValidateBusinessRules(decimal amount, int probability, string stage, DateOnly expectedCloseDate);
}

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public OpportunityService(ApplicationDbContext db, IScopeService scope, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _scope = scope;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<Opportunity> WithDetails() =>
        _db.Opportunities.Include(o => o.Customer).Include(o => o.Lead).Include(o => o.AssignedUser);

    public void ValidateBusinessRules(decimal amount, int probability, string stage, DateOnly expectedCloseDate)
    {
        var errors = new List<FieldError>();
        var isOpen = OpportunityStages.IsOpen(stage);

        if (amount < 0)
        {
            errors.Add(new FieldError("amount", "Opportunity Amount cannot be negative."));
        }
        else if (isOpen && amount <= 0)
        {
            errors.Add(new FieldError("amount", "Opportunity Amount must be greater than 0."));
        }

        if (probability < 0 || probability > 100)
        {
            errors.Add(new FieldError("probability", "Probability must be between 0 and 100."));
        }

        if (isOpen && expectedCloseDate < DateHelper.Today())
        {
            errors.Add(new FieldError("expectedCloseDate", "Expected Close Date cannot be in the past."));
        }

        if (errors.Count > 0) throw AppException.Fields(errors);
    }

    // Won is always 100% and Lost is always 0%
    private static int ProbabilityForStage(string stage, int probability) => stage switch
    {
        OpportunityStages.Won => 100,
        OpportunityStages.Lost => 0,
        _ => probability,
    };

    public async Task<PagedResult<OpportunityDto>> GetListAsync(OpportunityListQuery query)
    {
        var opportunities = await _scope.ApplyScopeAsync(WithDetails().AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            opportunities = opportunities.Where(o => EF.Functions.Like(o.OpportunityName, $"%{s}%"));
        }
        if (query.CustomerId.HasValue) opportunities = opportunities.Where(o => o.CustomerId == query.CustomerId);
        if (!string.IsNullOrEmpty(query.Stage)) opportunities = opportunities.Where(o => o.Stage == query.Stage);
        if (!string.IsNullOrEmpty(query.Status)) opportunities = opportunities.Where(o => o.Status == query.Status);
        if (query.AssignedTo.HasValue) opportunities = opportunities.Where(o => o.AssignedTo == query.AssignedTo);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<Opportunity, object?>>>
        {
            ["opportunityName"] = o => o.OpportunityName,
            ["amount"] = o => o.Amount,
            ["probability"] = o => o.Probability,
            ["expectedCloseDate"] = o => o.ExpectedCloseDate,
            ["stage"] = o => o.Stage,
            ["createdAt"] = o => o.CreatedDate,
        };

        return await opportunities.SortBy(query, sortColumns, "createdAt").ToPagedResultAsync(query, OpportunityDto.From);
    }

    // Data for the kanban style pipeline board
    public async Task<PipelineDto> GetPipelineAsync()
    {
        var query = await _scope.ApplyScopeAsync(WithDetails().AsNoTracking());
        var all = await query.OrderBy(o => o.ExpectedCloseDate).ToListAsync();

        var result = new PipelineDto();
        foreach (var stage in OpportunityStages.All)
        {
            var items = all.Where(o => o.Stage == stage).Select(OpportunityDto.From).ToList();
            result.Stages.Add(new PipelineStageDto
            {
                Stage = stage,
                Count = items.Count,
                Amount = items.Sum(o => o.Amount),
                WeightedAmount = items.Sum(o => o.WeightedAmount),
                Items = items,
            });
        }
        return result;
    }

    private async Task<Opportunity> GetEntityAsync(int id)
    {
        var opportunity = await WithDetails().FirstOrDefaultAsync(o => o.OpportunityId == id)
            ?? throw AppException.NotFound("Opportunity");
        await _scope.EnsureInScopeAsync(opportunity.AssignedTo);
        return opportunity;
    }

    public async Task<OpportunityDto> GetByIdAsync(int id) => OpportunityDto.From(await GetEntityAsync(id));

    public async Task<OpportunityDto> CreateAsync(OpportunityRequest request)
    {
        var user = await _currentUser.GetAsync();
        var probability = ProbabilityForStage(request.Stage!, request.Probability!.Value);
        ValidateBusinessRules(request.Amount!.Value, probability, request.Stage!, request.ExpectedCloseDate!.Value);
        await CheckReferencesAsync(request.CustomerId!.Value, request.LeadId);
        var assignedTo = await _scope.ValidateAssigneeAsync(request.AssignedTo);

        var opportunity = new Opportunity { AssignedTo = assignedTo, CreatedBy = user.Id };
        Apply(opportunity, request, probability);

        _db.Opportunities.Add(opportunity);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create,
            EntityName = "Opportunity",
            RecordId = opportunity.OpportunityId,
            NewValue = Snapshot(opportunity),
        });
        return await GetByIdAsync(opportunity.OpportunityId);
    }

    public async Task<OpportunityDto> UpdateAsync(int id, OpportunityRequest request)
    {
        var opportunity = await GetEntityAsync(id);
        if (opportunity.Status != "Open")
        {
            throw AppException.Conflict($"This opportunity is already {opportunity.Status} and cannot be edited.");
        }

        var probability = ProbabilityForStage(request.Stage!, request.Probability!.Value);
        ValidateBusinessRules(request.Amount!.Value, probability, request.Stage!, request.ExpectedCloseDate!.Value);
        await CheckReferencesAsync(request.CustomerId!.Value, request.LeadId);
        var assignedTo = request.AssignedTo is > 0 ? await _scope.ValidateAssigneeAsync(request.AssignedTo) : opportunity.AssignedTo;

        var before = Snapshot(opportunity);
        Apply(opportunity, request, probability);
        opportunity.AssignedTo = assignedTo;
        await _db.SaveChangesAsync();

        var (oldValues, newValues) = AuditService.Diff(before, Snapshot(opportunity));
        if (newValues.Count > 0)
        {
            var stageChanged = newValues.ContainsKey("stage");
            await _audit.LogAsync(new AuditEntry
            {
                Action = stageChanged ? AuditActions.StatusChange : AuditActions.Update,
                EntityName = "Opportunity",
                RecordId = id,
                OldValue = oldValues,
                NewValue = newValues,
                Details = stageChanged ? $"Stage changed from {oldValues["stage"]} to {newValues["stage"]}" : null,
            });
        }
        return await GetByIdAsync(id);
    }

    public async Task<OpportunityDto> ChangeStageAsync(int id, string stage)
    {
        var opportunity = await GetEntityAsync(id);
        if (opportunity.Status != "Open")
        {
            throw AppException.Conflict($"This opportunity is already {opportunity.Status} and cannot be moved.");
        }
        if (opportunity.Stage == stage) return OpportunityDto.From(opportunity);

        var probability = ProbabilityForStage(stage, opportunity.Probability);
        ValidateBusinessRules(opportunity.Amount, probability, stage, opportunity.ExpectedCloseDate);

        var oldStage = opportunity.Stage;
        opportunity.Probability = probability;
        opportunity.SetStage(stage);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.StatusChange,
            EntityName = "Opportunity",
            RecordId = id,
            OldValue = new { stage = oldStage },
            NewValue = new { stage, probability = opportunity.Probability },
            Details = $"Stage changed from {oldStage} to {stage}",
        });
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var opportunity = await GetEntityAsync(id);
        var snapshot = Snapshot(opportunity);
        _db.Opportunities.Remove(opportunity); // soft delete
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Delete,
            EntityName = "Opportunity",
            RecordId = id,
            OldValue = snapshot,
        });
    }

    // Customer must exist and be in the caller's scope, lead (optional) must exist
    private async Task CheckReferencesAsync(int customerId, int? leadId)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customerId)
            ?? throw AppException.Field("customerId", "Selected customer does not exist.");

        var scopeIds = await _scope.GetScopeUserIdsAsync();
        if (scopeIds != null && !scopeIds.Contains(customer.AssignedTo))
        {
            throw AppException.Field("customerId", "You do not have access to this customer.");
        }

        if (leadId is > 0 && !await _db.Leads.AnyAsync(l => l.LeadId == leadId))
        {
            throw AppException.Field("leadId", "Selected lead does not exist.");
        }
    }

    private static void Apply(Opportunity o, OpportunityRequest r, int probability)
    {
        o.OpportunityName = r.OpportunityName!.Trim();
        o.CustomerId = r.CustomerId!.Value;
        o.LeadId = r.LeadId is > 0 ? r.LeadId : null;
        o.Amount = r.Amount!.Value;
        o.Probability = probability;
        o.ExpectedCloseDate = r.ExpectedCloseDate!.Value;
        o.Source = string.IsNullOrWhiteSpace(r.Source) ? null : r.Source;
        o.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
        o.SetStage(r.Stage!);
    }

    private static Dictionary<string, object?> Snapshot(Opportunity o) => new()
    {
        ["opportunityName"] = o.OpportunityName,
        ["customerId"] = o.CustomerId,
        ["leadId"] = o.LeadId,
        ["amount"] = o.Amount,
        ["stage"] = o.Stage,
        ["probability"] = o.Probability,
        ["expectedCloseDate"] = o.ExpectedCloseDate.ToString("yyyy-MM-dd"),
        ["source"] = o.Source,
        ["notes"] = o.Notes,
        ["assignedTo"] = o.AssignedTo,
    };
}
