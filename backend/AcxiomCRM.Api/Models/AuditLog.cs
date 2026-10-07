namespace AcxiomCRM.Api.Models;

// Append-only table. ApplicationDbContext blocks updates and deletes of these rows.
public class AuditLog
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }      // copy of the name at the time of the event
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty; // module: Auth, User, Customer ...
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }      // JSON
    public string? NewValue { get; set; }      // JSON
    public string Result { get; set; } = "Success";
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
