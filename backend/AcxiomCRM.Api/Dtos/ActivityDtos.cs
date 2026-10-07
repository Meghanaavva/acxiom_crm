using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Validation;

namespace AcxiomCRM.Api.Dtos;

// ActivityDto (response) is in SharedDtos.cs

public class ActivityRequest
{
    [Required(ErrorMessage = "Activity Type is required.")]
    [OneOf(typeof(ActivityTypes))]
    [Display(Name = "Activity Type")]
    public string? ActivityType { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLen(150, "Subject")]
    public string? Subject { get; set; }

    [MaxLen(1000, "Description")]
    public string? Description { get; set; }

    // ISO date time from the client, e.g. 2026-10-07T09:30:00.000Z
    [Required(ErrorMessage = "Activity Date is required.")]
    public DateTime? ActivityDate { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [OneOf(typeof(ActivityStatuses))]
    public string? Status { get; set; }

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? AssignedTo { get; set; }
}

public class ActivityListQuery : PageQuery
{
    public string? ActivityType { get; set; }
    public string? Status { get; set; }
    public int? AssignedTo { get; set; }
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}
