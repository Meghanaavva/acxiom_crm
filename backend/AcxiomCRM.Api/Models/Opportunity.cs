namespace AcxiomCRM.Api.Models;

public class Opportunity : IAssignable, ISoftDelete
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public decimal Amount { get; set; }
    public string Stage { get; set; } = "Qualification";
    public int Probability { get; set; }
    public DateOnly ExpectedCloseDate { get; set; }
    public string? Source { get; set; }

    // Open / Won / Lost - always derived from Stage (see SetStage)
    public string Status { get; set; } = "Open";
    public DateTime? ClosedDate { get; set; }
    public string? Notes { get; set; }

    public int AssignedTo { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }

    // Keeps Stage, Status, Probability and ClosedDate consistent
    public void SetStage(string stage)
    {
        Stage = stage;
        if (stage == "Won" || stage == "Lost")
        {
            Status = stage;
            Probability = stage == "Won" ? 100 : 0;
            ClosedDate ??= DateTime.UtcNow;
        }
        else
        {
            Status = "Open";
            ClosedDate = null;
        }
    }
}
