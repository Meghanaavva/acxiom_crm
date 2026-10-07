using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Api.Models;

public class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }

    public ApplicationRole() { }

    public ApplicationRole(string name, string description) : base(name)
    {
        Description = description;
    }
}
