using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Validation;

namespace AcxiomCRM.Api.Dtos;

// FollowUpDto (response) is in SharedDtos.cs

public class FollowUpRequest
{
    [Required(ErrorMessage = "Subject is required.")]
    [MaxLen(150, "Subject")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [OneOf(typeof(FollowUpTypes))]
    [Display(Name = "Follow-up Type")]
    public string? FollowUpType { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    public DateOnly? FollowUpDate { get; set; }

    [MaxLen(1000, "Remarks")]
    public string? Remarks { get; set; }

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
    public int? AssignedTo { get; set; }
}

public class CompleteFollowUpRequest
{
    [MaxLen(1000, "Remarks")]
    public string? Remarks { get; set; }
}

public class RescheduleFollowUpRequest
{
    [Required(ErrorMessage = "Follow-up Date is required.")]
    public DateOnly? FollowUpDate { get; set; }

    [MaxLen(1000, "Remarks")]
    public string? Remarks { get; set; }
}

public class FollowUpListQuery : PageQuery
{
    public string? Status { get; set; }    // Planned / Completed / Missed / Cancelled / Overdue
    public string? View { get; set; }      // today / upcoming / overdue
    public int? AssignedTo { get; set; }
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}
