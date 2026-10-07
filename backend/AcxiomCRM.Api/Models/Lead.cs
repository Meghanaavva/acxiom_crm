namespace AcxiomCRM.Api.Models;

public class Lead : IAssignable, ISoftDelete
{
    public int LeadId { get; set; }
    public string? LeadCode { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";
    public decimal ExpectedValue { get; set; }
    public string? Notes { get; set; }

    public int AssignedTo { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
    public int CreatedBy { get; set; }

    public int? ConvertedCustomerId { get; set; }
    public Customer? ConvertedCustomer { get; set; }
    public DateTime? ConvertedDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
}
