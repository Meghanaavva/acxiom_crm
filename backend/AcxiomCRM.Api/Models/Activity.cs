namespace AcxiomCRM.Api.Models;

public class Activity : IAssignable, ISoftDelete
{
    public int ActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public string Status { get; set; } = "Planned";

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public int AssignedTo { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
}
