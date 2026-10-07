using AcxiomCRM.Api.Helpers;

namespace AcxiomCRM.Api.Dtos;

// AuditLogDto (response) is in SharedDtos.cs

public class AuditLogQuery : PageQuery
{
    public AuditLogQuery()
    {
        PageSize = 20; // audit screen shows more rows by default
    }

    public int? UserId { get; set; }
    public string? EntityName { get; set; }
    public string? Action { get; set; }
    public string? Result { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}

public class AuditFiltersDto
{
    public List<string> Actions { get; set; } = new();
    public List<string> Entities { get; set; } = new();
}
