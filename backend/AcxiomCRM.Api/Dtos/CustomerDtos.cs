using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using AcxiomCRM.Api.Validation;

namespace AcxiomCRM.Api.Dtos;

public class CustomerDto
{
    public int Id { get; set; }
    public string? CustomerCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public int CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static CustomerDto From(Customer c) => new()
    {
        Id = c.CustomerId,
        CustomerCode = c.CustomerCode,
        CustomerName = c.CustomerName,
        Email = c.Email,
        Phone = c.Phone,
        CompanyName = c.CompanyName,
        Address = c.Address,
        City = c.City,
        State = c.State,
        Status = c.Status,
        Notes = c.Notes,
        AssignedTo = c.AssignedTo,
        AssignedToName = c.AssignedUser?.FullName,
        CreatedBy = c.CreatedBy,
        CreatedByName = c.Creator?.FullName,
        CreatedAt = c.CreatedDate,
        UpdatedAt = c.ModifiedDate,
    };
}

// Used for both create and update
public class CustomerRequest
{
    [Required(ErrorMessage = "Customer Name is required.")]
    [MaxLen(100, "Customer Name")]
    public string? CustomerName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [ValidEmail]
    [MaxLen(150, "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone is required.")]
    [ValidPhone]
    public string? Phone { get; set; }

    [MaxLen(100, "Company Name")]
    public string? CompanyName { get; set; }

    [MaxLen(250, "Address")]
    public string? Address { get; set; }

    [MaxLen(50, "City")]
    public string? City { get; set; }

    [MaxLen(50, "State")]
    public string? State { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [OneOf(typeof(CustomerStatuses))]
    public string? Status { get; set; }

    [MaxLen(1000, "Notes")]
    public string? Notes { get; set; }

    public int? AssignedTo { get; set; }
}

public class CustomerListQuery : PageQuery
{
    public string? Status { get; set; }
    public int? AssignedTo { get; set; }
}

public class CustomerHistoryDto
{
    public List<OpportunityDto> Opportunities { get; set; } = new();
    public List<FollowUpDto> FollowUps { get; set; } = new();
    public List<ActivityDto> Activities { get; set; } = new();
    public ConvertedFromLeadDto? ConvertedFromLead { get; set; }
    public List<AuditLogDto> AuditLogs { get; set; } = new();
}

public record ConvertedFromLeadDto(int Id, string? LeadCode, string LeadName, DateTime? ConvertedAt);
