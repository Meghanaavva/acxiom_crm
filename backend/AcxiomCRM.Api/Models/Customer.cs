namespace AcxiomCRM.Api.Models;

public class Customer : IAssignable, ISoftDelete
{
    public int CustomerId { get; set; }
    public string? CustomerCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    public int AssignedTo { get; set; }
    public ApplicationUser? AssignedUser { get; set; }

    public int CreatedBy { get; set; }
    public ApplicationUser? Creator { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
