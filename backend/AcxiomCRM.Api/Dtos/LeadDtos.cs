using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Validation;

namespace AcxiomCRM.Api.Dtos;

// Used for both create and update. "At least email or phone" is checked in LeadService.
public class LeadRequest
{
    [Required(ErrorMessage = "Lead Name is required.")]
    [MaxLen(100, "Lead Name")]
    public string? LeadName { get; set; }

    [ValidEmail]
    [MaxLen(150, "Email")]
    public string? Email { get; set; }

    [ValidPhone]
    public string? Phone { get; set; }

    [MaxLen(100, "Company Name")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Source is required.")]
    [OneOf(typeof(LeadSources))]
    public string? Source { get; set; }

    [OneOf(typeof(LeadStatuses))]
    public string? Status { get; set; }

    [Required(ErrorMessage = "Priority is required.")]
    [OneOf(typeof(LeadPriorities))]
    public string? Priority { get; set; }

    [Range(0d, (double)ValidationRules.LeadMaxExpectedValue, ErrorMessage = "Expected Value must be between 0 and 100000000.")]
    public decimal? ExpectedValue { get; set; }

    [MaxLen(1000, "Notes")]
    public string? Notes { get; set; }

    public int? AssignedTo { get; set; }
}

public class LeadStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [OneOf(typeof(LeadStatuses))]
    public string? Status { get; set; }
}

// Opportunity fields are only required when CreateOpportunity is true (checked in LeadService)
public class ConvertLeadRequest
{
    public bool CreateOpportunity { get; set; }

    [MaxLen(150, "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Range(0d, 999999999999d, ErrorMessage = "Opportunity Amount must be a valid number and cannot be negative.")]
    public decimal? Amount { get; set; }

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int? Probability { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }
}

public class ConvertLeadResponse
{
    public LeadDto Lead { get; set; } = new();
    public CustomerDto Customer { get; set; } = new();
    public OpportunityDto? Opportunity { get; set; }
}

public class LeadListQuery : PageQuery
{
    public string? Status { get; set; }
    public string? Source { get; set; }
    public string? Priority { get; set; }
    public int? AssignedTo { get; set; }
}
