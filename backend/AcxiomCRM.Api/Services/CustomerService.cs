using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetListAsync(CustomerListQuery query);
    Task<CustomerDto> GetByIdAsync(int id);
    Task<CustomerHistoryDto> GetHistoryAsync(int id);
    Task<CustomerDto> CreateAsync(CustomerRequest request);
    Task<CustomerDto> UpdateAsync(int id, CustomerRequest request);
    Task DeleteAsync(int id);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public CustomerService(ApplicationDbContext db, IScopeService scope, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _scope = scope;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<Customer> WithUsers() =>
        _db.Customers.Include(c => c.AssignedUser).Include(c => c.Creator);

    public async Task<PagedResult<CustomerDto>> GetListAsync(CustomerListQuery query)
    {
        var customers = await _scope.ApplyScopeAsync(WithUsers().AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            customers = customers.Where(c =>
                EF.Functions.Like(c.CustomerName, $"%{s}%") ||
                EF.Functions.Like(c.Email, $"%{s}%") ||
                EF.Functions.Like(c.Phone, $"%{s}%") ||
                EF.Functions.Like(c.CompanyName!, $"%{s}%") ||
                EF.Functions.Like(c.CustomerCode!, $"%{s}%"));
        }
        if (!string.IsNullOrEmpty(query.Status)) customers = customers.Where(c => c.Status == query.Status);
        if (query.AssignedTo.HasValue) customers = customers.Where(c => c.AssignedTo == query.AssignedTo);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<Customer, object?>>>
        {
            ["customerName"] = c => c.CustomerName,
            ["customerCode"] = c => c.CustomerCode,
            ["email"] = c => c.Email,
            ["companyName"] = c => c.CompanyName,
            ["status"] = c => c.Status,
            ["createdAt"] = c => c.CreatedDate,
        };

        return await customers.SortBy(query, sortColumns, "createdAt").ToPagedResultAsync(query, CustomerDto.From);
    }

    // Loads a customer and checks that the user is allowed to see it
    private async Task<Customer> GetEntityAsync(int id)
    {
        var customer = await WithUsers().FirstOrDefaultAsync(c => c.CustomerId == id)
            ?? throw AppException.NotFound("Customer");
        await _scope.EnsureInScopeAsync(customer.AssignedTo);
        return customer;
    }

    public async Task<CustomerDto> GetByIdAsync(int id) => CustomerDto.From(await GetEntityAsync(id));

    public async Task<CustomerDto> CreateAsync(CustomerRequest request)
    {
        var user = await _currentUser.GetAsync();
        var email = request.Email!.Trim().ToLower();
        var phone = request.Phone!.Trim();
        await CheckDuplicateAsync(email, phone, null);
        var assignedTo = await _scope.ValidateAssigneeAsync(request.AssignedTo);

        var customer = new Customer { CreatedBy = user.Id, AssignedTo = assignedTo };
        Apply(customer, request, email, phone);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        // Customer code like CUS-00012 is generated from the id
        customer.CustomerCode = $"CUS-{customer.CustomerId:D5}";
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create,
            EntityName = "Customer",
            RecordId = customer.CustomerId,
            NewValue = Snapshot(customer),
        });
        return await GetByIdAsync(customer.CustomerId);
    }

    public async Task<CustomerDto> UpdateAsync(int id, CustomerRequest request)
    {
        var customer = await GetEntityAsync(id);
        var email = request.Email!.Trim().ToLower();
        var phone = request.Phone!.Trim();
        await CheckDuplicateAsync(email, phone, id);
        var assignedTo = request.AssignedTo is > 0 ? await _scope.ValidateAssigneeAsync(request.AssignedTo) : customer.AssignedTo;

        var before = Snapshot(customer);
        Apply(customer, request, email, phone);
        customer.AssignedTo = assignedTo;
        await _db.SaveChangesAsync();

        var (oldValues, newValues) = AuditService.Diff(before, Snapshot(customer));
        if (newValues.Count > 0)
        {
            var statusChanged = newValues.ContainsKey("status");
            await _audit.LogAsync(new AuditEntry
            {
                Action = statusChanged ? AuditActions.StatusChange : AuditActions.Update,
                EntityName = "Customer",
                RecordId = id,
                OldValue = oldValues,
                NewValue = newValues,
                Details = statusChanged ? $"Status changed from {oldValues["status"]} to {newValues["status"]}" : null,
            });
        }
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await GetEntityAsync(id);
        var hasOpenOpportunities = await _db.Opportunities.AnyAsync(o => o.CustomerId == id && o.Status == "Open");
        if (hasOpenOpportunities)
        {
            throw AppException.Conflict("This customer has open opportunities. Close or delete them first.");
        }

        _db.Customers.Remove(customer); // soft delete (see ApplicationDbContext)
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Delete,
            EntityName = "Customer",
            RecordId = id,
            OldValue = Snapshot(customer),
        });
    }

    // Everything linked to one customer - for the details page
    public async Task<CustomerHistoryDto> GetHistoryAsync(int id)
    {
        await GetEntityAsync(id);

        var opportunities = await _db.Opportunities.AsNoTracking()
            .Include(o => o.Customer).Include(o => o.AssignedUser)
            .Where(o => o.CustomerId == id).OrderByDescending(o => o.CreatedDate).ToListAsync();
        var followUps = await _db.FollowUps.AsNoTracking()
            .Include(f => f.AssignedUser)
            .Where(f => f.CustomerId == id).OrderByDescending(f => f.FollowUpDate).ToListAsync();
        var activities = await _db.Activities.AsNoTracking()
            .Include(a => a.AssignedUser)
            .Where(a => a.CustomerId == id).OrderByDescending(a => a.ActivityDate).ToListAsync();
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.ConvertedCustomerId == id);
        var logs = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == "Customer" && a.RecordId == id.ToString())
            .OrderByDescending(a => a.CreatedDate).Take(50).ToListAsync();

        return new CustomerHistoryDto
        {
            Opportunities = opportunities.Select(OpportunityDto.From).ToList(),
            FollowUps = followUps.Select(FollowUpDto.From).ToList(),
            Activities = activities.Select(ActivityDto.From).ToList(),
            ConvertedFromLead = lead == null ? null : new ConvertedFromLeadDto(lead.LeadId, lead.LeadCode, lead.LeadName, lead.ConvertedDate),
            AuditLogs = logs.Select(AuditLogDto.From).ToList(),
        };
    }

    // Same email or same phone = same customer. Deleted customers are checked too (unique index).
    private async Task CheckDuplicateAsync(string email, string phone, int? excludeId)
    {
        var existing = await _db.Customers.IgnoreQueryFilters()
            .Where(c => (c.Email == email || c.Phone == phone) && c.CustomerId != excludeId)
            .Select(c => new { c.Email, c.Phone })
            .FirstOrDefaultAsync();
        if (existing == null) return;

        var errors = new List<FieldError>();
        if (existing.Email == email) errors.Add(new FieldError("email", "A customer with this email already exists."));
        if (existing.Phone == phone) errors.Add(new FieldError("phone", "A customer with this phone number already exists."));
        throw AppException.Conflict("Duplicate customer.", errors);
    }

    private static void Apply(Customer c, CustomerRequest r, string email, string phone)
    {
        c.CustomerName = r.CustomerName!.Trim();
        c.Email = email;
        c.Phone = phone;
        c.CompanyName = Clean(r.CompanyName);
        c.Address = Clean(r.Address);
        c.City = Clean(r.City);
        c.State = Clean(r.State);
        c.Status = r.Status!;
        c.Notes = Clean(r.Notes);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Dictionary<string, object?> Snapshot(Customer c) => new()
    {
        ["customerName"] = c.CustomerName,
        ["email"] = c.Email,
        ["phone"] = c.Phone,
        ["companyName"] = c.CompanyName,
        ["address"] = c.Address,
        ["city"] = c.City,
        ["state"] = c.State,
        ["status"] = c.Status,
        ["notes"] = c.Notes,
        ["assignedTo"] = c.AssignedTo,
    };
}
