namespace AcxiomCRM.Api.Helpers;

// All fixed values used in the app. The React app keeps the same values in src/utils/constants.ts

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";

    public const string AdminOrManager = Admin + "," + Manager;
    public static readonly string[] All = { Admin, Manager, SalesExecutive };
}

public static class CustomerStatuses
{
    public static readonly string[] All = { "Active", "Inactive", "Prospect" };
}

public static class LeadStatuses
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Unqualified = "Unqualified";
    public const string Converted = "Converted";
    public const string Lost = "Lost";

    public static readonly string[] All = { New, Contacted, Qualified, Unqualified, Converted, Lost };

    // Statuses allowed when a lead is created
    public static readonly string[] AllowedOnCreate = { New, Contacted, Qualified };

    // Allowed status changes. "Converted" is only set by the convert action.
    public static readonly Dictionary<string, string[]> Transitions = new()
    {
        [New] = new[] { Contacted, Unqualified, Lost },
        [Contacted] = new[] { Qualified, Unqualified, Lost },
        [Qualified] = new[] { Lost },
        [Unqualified] = new[] { Contacted, Lost },
        [Converted] = Array.Empty<string>(),
        [Lost] = Array.Empty<string>(),
    };

    public static bool CanChange(string from, string to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

public static class LeadSources
{
    public static readonly string[] All = { "Website", "Referral", "Social Media", "Email Campaign", "Cold Call", "Event", "Other" };
}

public static class LeadPriorities
{
    public static readonly string[] All = { "Low", "Medium", "High" };
}

public static class OpportunityStages
{
    public const string Qualification = "Qualification";
    public const string Proposal = "Proposal";
    public const string Negotiation = "Negotiation";
    public const string Won = "Won";
    public const string Lost = "Lost";

    public static readonly string[] All = { Qualification, Proposal, Negotiation, Won, Lost };
    public static readonly string[] Open = { Qualification, Proposal, Negotiation };

    public static bool IsOpen(string stage) => Open.Contains(stage);
}

public static class FollowUpTypes
{
    public static readonly string[] All = { "Call", "Meeting", "Email", "Visit", "Other" };
}

public static class FollowUpStatuses
{
    public const string Planned = "Planned";
    public const string Completed = "Completed";
    public const string Missed = "Missed";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = { Planned, Completed, Missed, Cancelled };
}

public static class ActivityTypes
{
    public static readonly string[] All = { "Call", "Meeting", "Email", "Task" };
}

public static class ActivityStatuses
{
    public static readonly string[] All = { "Planned", "Completed", "Cancelled" };
}

public static class AuditActions
{
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string Lockout = "Lockout";
    public const string Unlock = "Unlock";
    public const string Register = "Register";
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string StatusChange = "StatusChange";
    public const string RoleChange = "RoleChange";
    public const string PasswordChange = "PasswordChange";
    public const string PasswordReset = "PasswordReset";
    public const string Convert = "Convert";
    public const string Complete = "Complete";
    public const string Reschedule = "Reschedule";
}

public static class ValidationRules
{
    // 10 digit Indian mobile number starting with 6-9
    public const string PhonePattern = @"^[6-9]\d{9}$";
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";
    public const decimal LeadMaxExpectedValue = 100_000_000m; // 10 crore
    public const decimal MaxAmount = 10_000_000_000m;
}
