namespace AcxiomCRM.Api.Models;

// Every CRM record that belongs to a user implements this, so data scope
// (own / team / all) can be applied in one place - see ScopeService.
public interface IAssignable
{
    int AssignedTo { get; set; }
}

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedDate { get; set; }
}
