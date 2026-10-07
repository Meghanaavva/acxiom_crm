using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Validation;

namespace AcxiomCRM.Api.Dtos;

// Field format checks only. Business rules (amount > 0 for active, close date not in the past)
// are checked in OpportunityService.ValidateBusinessRules.
public class OpportunityRequest
{
    [Required(ErrorMessage = "Opportunity Name is required.")]
    [MaxLen(150, "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Required(ErrorMessage = "Customer is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Customer is invalid.")]
    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0d, 999999999999d, ErrorMessage = "Opportunity Amount must be a valid number and cannot be negative.")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    [OneOf(typeof(OpportunityStages))]
    public string? Stage { get; set; }

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int? Probability { get; set; }

    [Required(ErrorMessage = "Expected Close Date is required.")]
    public DateOnly? ExpectedCloseDate { get; set; }

    [OneOf(typeof(LeadSources))]
    public string? Source { get; set; }

    [MaxLen(1000, "Notes")]
    public string? Notes { get; set; }

    public int? AssignedTo { get; set; }
}

public class OpportunityStageRequest
{
    [Required(ErrorMessage = "Stage is required.")]
    [OneOf(typeof(OpportunityStages))]
    public string? Stage { get; set; }
}

public class OpportunityListQuery : PageQuery
{
    public int? CustomerId { get; set; }
    public string? Stage { get; set; }
    public string? Status { get; set; }
    public int? AssignedTo { get; set; }
}

public class PipelineDto
{
    public List<PipelineStageDto> Stages { get; set; } = new();
}

public class PipelineStageDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal WeightedAmount { get; set; }
    public List<OpportunityDto> Items { get; set; } = new();
}
