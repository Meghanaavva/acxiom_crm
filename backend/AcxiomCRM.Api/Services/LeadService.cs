using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface ILeadService
{
    Task<PagedResult<LeadDto>> GetListAsync(LeadListQuery query);
    Task<LeadDto> GetByIdAsync(int id);
    Task<LeadDto> CreateAsync(LeadRequest request);
    Task<LeadDto> UpdateAsync(int id, LeadRequest request);
    Task<LeadDto> ChangeStatusAsync(int id, string status);
    Task<ConvertLeadResponse> ConvertAsync(int id, ConvertLeadRequest request);
    Task DeleteAsync(int id);
}

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly IOpportunityService _opportunityService;

    public LeadService(ApplicationDbContext db, IScopeService scope, ICurrentUserService currentUser, IAuditService audit,
        IOpportunityService opportunityService)
    {
        _db = db;
        _scope = scope;
        _currentUser = currentUser;
        _audit = audit;
        _opportunityService = opportunityService;
    }

    private IQueryable<Lead> WithDetails() =>
        _db.Leads.Include(l => l.AssignedUser).Include(l => l.ConvertedCustomer);

    public async Task<PagedResult<LeadDto>> GetListAsync(LeadListQuery query)
    {
        var leads = await _scope.ApplyScopeAsync(WithDetails().AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            leads = leads.Where(l =>
                EF.Functions.Like(l.LeadName, $"%{s}%") ||
                EF.Functions.Like(l.CompanyName!, $"%{s}%") ||
                EF.Functions.Like(l.Email!, $"%{s}%") ||
                EF.Functions.Like(l.Phone!, $"%{s}%") ||
                EF.Functions.Like(l.LeadCode!, $"%{s}%"));
        }
        if (!string.IsNullOrEmpty(query.Status)) leads = leads.Where(l => l.Status == query.Status);
        if (!string.IsNullOrEmpty(query.Source)) leads = leads.Where(l => l.Source == query.Source);
        if (!string.IsNullOrEmpty(query.Priority)) leads = leads.Where(l => l.Priority == query.Priority);
        if (query.AssignedTo.HasValue) leads = leads.Where(l => l.AssignedTo == query.AssignedTo);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<Lead, object?>>>
        {
            ["leadName"] = l => l.LeadName,
            ["leadCode"] = l => l.LeadCode,
            ["companyName"] = l => l.CompanyName,
            ["status"] = l => l.Status,
            ["priority"] = l => l.Priority,
            ["expectedValue"] = l => l.ExpectedValue,
            ["createdAt"] = l => l.CreatedDate,
        };

        return await leads.SortBy(query, sortColumns, "createdAt").ToPagedResultAsync(query, LeadDto.From);
    }

    private async Task<Lead> GetEntityAsync(int id)
    {
        var lead = await WithDetails().FirstOrDefaultAsync(l => l.LeadId == id) ?? throw AppException.NotFound("Lead");
        await _scope.EnsureInScopeAsync(lead.AssignedTo);
        return lead;
    }

    public async Task<LeadDto> GetByIdAsync(int id) => LeadDto.From(await GetEntityAsync(id));

    public async Task<LeadDto> CreateAsync(LeadRequest request)
    {
        var user = await _currentUser.GetAsync();
        CheckContactDetails(request);

        var status = string.IsNullOrEmpty(request.Status) ? LeadStatuses.New : request.Status;
        if (!LeadStatuses.AllowedOnCreate.Contains(status))
        {
            throw AppException.Field("status", $"A new lead can only be {string.Join(", ", LeadStatuses.AllowedOnCreate)}.");
        }
        var assignedTo = await _scope.ValidateAssigneeAsync(request.AssignedTo);

        var lead = new Lead { Status = status, AssignedTo = assignedTo, CreatedBy = user.Id };
        Apply(lead, request);

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        // Lead code like LEAD-00012 is generated from the id
        lead.LeadCode = $"LEAD-{lead.LeadId:D5}";
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create,
            EntityName = "Lead",
            RecordId = lead.LeadId,
            NewValue = Snapshot(lead),
        });
        return await GetByIdAsync(lead.LeadId);
    }

    public async Task<LeadDto> UpdateAsync(int id, LeadRequest request)
    {
        var lead = await GetEntityAsync(id);
        EnsureEditable(lead);
        CheckContactDetails(request);

        var status = string.IsNullOrEmpty(request.Status) ? lead.Status : request.Status;
        CheckTransition(lead.Status, status);
        var assignedTo = request.AssignedTo is > 0 ? await _scope.ValidateAssigneeAsync(request.AssignedTo) : lead.AssignedTo;

        var before = Snapshot(lead);
        Apply(lead, request);
        lead.Status = status;
        lead.AssignedTo = assignedTo;
        await _db.SaveChangesAsync();

        var (oldValues, newValues) = AuditService.Diff(before, Snapshot(lead));
        if (newValues.Count > 0)
        {
            var statusChanged = newValues.ContainsKey("status");
            await _audit.LogAsync(new AuditEntry
            {
                Action = statusChanged ? AuditActions.StatusChange : AuditActions.Update,
                EntityName = "Lead",
                RecordId = id,
                OldValue = oldValues,
                NewValue = newValues,
                Details = statusChanged ? $"Status changed from {oldValues["status"]} to {newValues["status"]}" : null,
            });
        }
        return await GetByIdAsync(id);
    }

    public async Task<LeadDto> ChangeStatusAsync(int id, string status)
    {
        var lead = await GetEntityAsync(id);
        EnsureEditable(lead);
        CheckTransition(lead.Status, status);
        if (lead.Status == status) return LeadDto.From(lead);

        var oldStatus = lead.Status;
        lead.Status = status;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.StatusChange,
            EntityName = "Lead",
            RecordId = id,
            OldValue = new { status = oldStatus },
            NewValue = new { status },
            Details = $"Status changed from {oldStatus} to {status}",
        });
        return await GetByIdAsync(id);
    }

    /// <summary>
    /// Qualified lead -> Customer (+ optional Opportunity).
    /// Runs in a transaction so we never end up with half converted data.
    /// </summary>
    public async Task<ConvertLeadResponse> ConvertAsync(int id, ConvertLeadRequest request)
    {
        var user = await _currentUser.GetAsync();
        var lead = await GetEntityAsync(id);
        if (lead.Status != LeadStatuses.Qualified)
        {
            throw AppException.Conflict("Only qualified leads can be converted.");
        }

        if (request.CreateOpportunity)
        {
            CheckOpportunityFields(request);
            _opportunityService.ValidateBusinessRules(request.Amount!.Value, request.Probability!.Value,
                OpportunityStages.Qualification, request.ExpectedCloseDate!.Value);
        }

        // Reuse an existing customer with the same email/phone instead of creating a duplicate
        var existingCustomer = await _db.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => (lead.Email != null && c.Email == lead.Email) || (lead.Phone != null && c.Phone == lead.Phone));

        if (existingCustomer == null)
        {
            var errors = new List<FieldError>();
            if (string.IsNullOrEmpty(lead.Email))
                errors.Add(new FieldError("email", "Lead email is required to create a customer. Edit the lead first."));
            if (string.IsNullOrEmpty(lead.Phone))
                errors.Add(new FieldError("phone", "Lead phone is required to create a customer. Edit the lead first."));
            if (errors.Count > 0) throw AppException.BadRequest("Lead needs both email and phone to be converted.", errors);
        }

        var customerCreated = false;
        Customer customer;
        Opportunity? opportunity = null;

        await using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            if (existingCustomer != null)
            {
                customer = existingCustomer;
                if (customer.IsDeleted)
                {
                    // bring back a previously deleted customer
                    customer.IsDeleted = false;
                    customer.DeletedDate = null;
                }
            }
            else
            {
                customer = new Customer
                {
                    CustomerName = lead.LeadName,
                    Email = lead.Email!,
                    Phone = lead.Phone!,
                    CompanyName = lead.CompanyName,
                    Status = "Active",
                    Notes = $"Converted from lead {lead.LeadCode}",
                    AssignedTo = lead.AssignedTo,
                    CreatedBy = user.Id,
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
                customer.CustomerCode = $"CUS-{customer.CustomerId:D5}";
                customerCreated = true;
            }

            if (request.CreateOpportunity)
            {
                opportunity = new Opportunity
                {
                    OpportunityName = request.OpportunityName!.Trim(),
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = request.Amount!.Value,
                    Probability = request.Probability!.Value,
                    ExpectedCloseDate = request.ExpectedCloseDate!.Value,
                    Source = lead.Source,
                    AssignedTo = lead.AssignedTo,
                    CreatedBy = user.Id,
                };
                opportunity.SetStage(OpportunityStages.Qualification);
                _db.Opportunities.Add(opportunity);
            }

            lead.Status = LeadStatuses.Converted;
            lead.ConvertedCustomerId = customer.CustomerId;
            lead.ConvertedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Audit entries are written after the transaction is committed
        if (customerCreated)
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.Create,
                EntityName = "Customer",
                RecordId = customer.CustomerId,
                Details = $"Created from lead {lead.LeadCode}",
            });
        }
        if (opportunity != null)
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.Create,
                EntityName = "Opportunity",
                RecordId = opportunity.OpportunityId,
                Details = $"Created from lead {lead.LeadCode}",
            });
        }
        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Convert,
            EntityName = "Lead",
            RecordId = id,
            OldValue = new { status = LeadStatuses.Qualified },
            NewValue = new { status = LeadStatuses.Converted, customerId = customer.CustomerId, opportunityId = opportunity?.OpportunityId },
            Details = $"Lead {lead.LeadCode} converted to customer #{customer.CustomerId}",
        });

        // Reload with related names for the response
        var savedCustomer = await _db.Customers.AsNoTracking()
            .Include(c => c.AssignedUser).Include(c => c.Creator)
            .FirstAsync(c => c.CustomerId == customer.CustomerId);
        OpportunityDto? opportunityDto = null;
        if (opportunity != null)
        {
            var savedOpportunity = await _db.Opportunities.AsNoTracking()
                .Include(o => o.Customer).Include(o => o.Lead).Include(o => o.AssignedUser)
                .FirstAsync(o => o.OpportunityId == opportunity.OpportunityId);
            opportunityDto = OpportunityDto.From(savedOpportunity);
        }

        return new ConvertLeadResponse
        {
            Lead = await GetByIdAsync(id),
            Customer = CustomerDto.From(savedCustomer),
            Opportunity = opportunityDto,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var lead = await GetEntityAsync(id);
        var snapshot = Snapshot(lead);
        _db.Leads.Remove(lead); // soft delete
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Delete,
            EntityName = "Lead",
            RecordId = id,
            OldValue = snapshot,
        });
    }

    // A lead needs at least one way to contact them
    private static void CheckContactDetails(LeadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            throw AppException.Field("email", "Enter at least an email or a phone number.");
        }
    }

    // Throws when the status change is not allowed by the workflow
    private static void CheckTransition(string from, string to)
    {
        if (from == to) return;
        if (to == LeadStatuses.Converted)
        {
            throw AppException.BadRequest("Use the Convert action to convert a qualified lead.",
                new List<FieldError> { new("status", "Use the Convert action to convert a qualified lead.") });
        }
        if (!LeadStatuses.CanChange(from, to))
        {
            var message = $"Lead status cannot change from {from} to {to}.";
            throw AppException.BadRequest(message, new List<FieldError> { new("status", message) });
        }
    }

    private static void EnsureEditable(Lead lead)
    {
        if (lead.Status == LeadStatuses.Converted || lead.Status == LeadStatuses.Lost)
        {
            throw AppException.Conflict($"This lead is already {lead.Status} and cannot be changed.");
        }
    }

    private static void CheckOpportunityFields(ConvertLeadRequest request)
    {
        var errors = new List<FieldError>();
        if (string.IsNullOrWhiteSpace(request.OpportunityName))
            errors.Add(new FieldError("opportunityName", "Opportunity Name is required."));
        if (request.Amount == null)
            errors.Add(new FieldError("amount", "Opportunity Amount is required."));
        if (request.Probability == null)
            errors.Add(new FieldError("probability", "Probability is required."));
        if (request.ExpectedCloseDate == null)
            errors.Add(new FieldError("expectedCloseDate", "Expected Close Date is required."));
        if (errors.Count > 0) throw AppException.Fields(errors);
    }

    private static void Apply(Lead lead, LeadRequest r)
    {
        lead.LeadName = r.LeadName!.Trim();
        lead.Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim().ToLower();
        lead.Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
        lead.CompanyName = string.IsNullOrWhiteSpace(r.CompanyName) ? null : r.CompanyName.Trim();
        lead.Source = r.Source!;
        lead.Priority = r.Priority!;
        lead.ExpectedValue = r.ExpectedValue ?? 0;
        lead.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
    }

    private static Dictionary<string, object?> Snapshot(Lead l) => new()
    {
        ["leadName"] = l.LeadName,
        ["email"] = l.Email,
        ["phone"] = l.Phone,
        ["companyName"] = l.CompanyName,
        ["source"] = l.Source,
        ["status"] = l.Status,
        ["priority"] = l.Priority,
        ["expectedValue"] = l.ExpectedValue,
        ["notes"] = l.Notes,
        ["assignedTo"] = l.AssignedTo,
    };
}
