using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;

namespace AcxiomCRM.Api.Dtos;

// DTOs used by more than one module. Request DTOs for these live in their own module files.

public class LeadDto
{
    public int Id { get; set; }
    public string? LeadCode { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public string? Notes { get; set; }
    public int AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public int CreatedBy { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public string? ConvertedCustomerName { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static LeadDto From(Lead l) => new()
    {
        Id = l.LeadId,
        LeadCode = l.LeadCode,
        LeadName = l.LeadName,
        Email = l.Email,
        Phone = l.Phone,
        CompanyName = l.CompanyName,
        Source = l.Source,
        Status = l.Status,
        Priority = l.Priority,
        ExpectedValue = l.ExpectedValue,
        Notes = l.Notes,
        AssignedTo = l.AssignedTo,
        AssignedToName = l.AssignedUser?.FullName,
        CreatedBy = l.CreatedBy,
        ConvertedCustomerId = l.ConvertedCustomerId,
        ConvertedCustomerName = l.ConvertedCustomer?.CustomerName,
        ConvertedAt = l.ConvertedDate,
        CreatedAt = l.CreatedDate,
        UpdatedAt = l.ModifiedDate,
    };
}

public class OpportunityDto
{
    public int Id { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedAmount { get; set; }
    public DateOnly ExpectedCloseDate { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
    public string? Notes { get; set; }
    public int AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Weighted pipeline = Amount x Probability / 100
    public static decimal Weighted(decimal amount, int probability) => Math.Round(amount * probability / 100m, 2);

    public static OpportunityDto From(Opportunity o) => new()
    {
        Id = o.OpportunityId,
        OpportunityName = o.OpportunityName,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.CustomerName,
        LeadId = o.LeadId,
        LeadName = o.Lead?.LeadName,
        Amount = o.Amount,
        Stage = o.Stage,
        Probability = o.Probability,
        WeightedAmount = Weighted(o.Amount, o.Probability),
        ExpectedCloseDate = o.ExpectedCloseDate,
        Source = o.Source,
        Status = o.Status,
        ClosedAt = o.ClosedDate,
        Notes = o.Notes,
        AssignedTo = o.AssignedTo,
        AssignedToName = o.AssignedUser?.FullName,
        CreatedBy = o.CreatedBy,
        CreatedAt = o.CreatedDate,
        UpdatedAt = o.ModifiedDate,
    };
}

public class FollowUpDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FollowUpType { get; set; } = string.Empty;
    public DateOnly FollowUpDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public string? Remarks { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public int AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static FollowUpDto From(FollowUp f) => new()
    {
        Id = f.FollowUpId,
        Subject = f.Subject,
        FollowUpType = f.FollowUpType,
        FollowUpDate = f.FollowUpDate,
        Status = f.Status,
        // planned follow-ups whose date has already passed
        IsOverdue = f.Status == FollowUpStatuses.Planned && f.FollowUpDate < DateHelper.Today(),
        Remarks = f.Remarks,
        CustomerId = f.CustomerId,
        CustomerName = f.Customer?.CustomerName,
        LeadId = f.LeadId,
        LeadName = f.Lead?.LeadName,
        OpportunityId = f.OpportunityId,
        OpportunityName = f.Opportunity?.OpportunityName,
        AssignedTo = f.AssignedTo,
        AssignedToName = f.AssignedUser?.FullName,
        CreatedBy = f.CreatedBy,
        CompletedAt = f.CompletedDate,
        CreatedAt = f.CreatedDate,
        UpdatedAt = f.ModifiedDate,
    };
}

public class ActivityDto
{
    public int Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ActivityDto From(Activity a) => new()
    {
        Id = a.ActivityId,
        ActivityType = a.ActivityType,
        Subject = a.Subject,
        Description = a.Description,
        ActivityDate = a.ActivityDate,
        Status = a.Status,
        CustomerId = a.CustomerId,
        CustomerName = a.Customer?.CustomerName,
        LeadId = a.LeadId,
        LeadName = a.Lead?.LeadName,
        AssignedTo = a.AssignedTo,
        AssignedToName = a.AssignedUser?.FullName,
        CreatedBy = a.CreatedBy,
        CreatedAt = a.CreatedDate,
        UpdatedAt = a.ModifiedDate,
    };
}

public class AuditLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }

    public static AuditLogDto From(AuditLog a) => new()
    {
        Id = a.AuditLogId,
        UserId = a.UserId,
        UserName = a.UserName,
        Action = a.Action,
        EntityName = a.EntityName,
        RecordId = a.RecordId,
        OldValue = a.OldValue,
        NewValue = a.NewValue,
        Result = a.Result,
        Details = a.Details,
        IpAddress = a.IpAddress,
        CreatedAt = a.CreatedDate,
    };
}
