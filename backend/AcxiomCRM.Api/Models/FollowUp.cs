namespace AcxiomCRM.Api.Models;

public class FollowUp : IAssignable, ISoftDelete
{
    public int FollowUpId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FollowUpType { get; set; } = string.Empty;
    public DateOnly FollowUpDate { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Remarks { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public int? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }

    public int AssignedTo { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? CompletedDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
}
