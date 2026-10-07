namespace AcxiomCRM.Api.Models;

// Previous password hashes of a user (Identity hashes, never plain text).
// Used to stop users from re-using their recent passwords.
public class PasswordHistory
{
    public int PasswordHistoryId { get; set; }
    public int UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
