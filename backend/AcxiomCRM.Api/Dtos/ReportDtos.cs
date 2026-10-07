using AcxiomCRM.Api.Helpers;

namespace AcxiomCRM.Api.Dtos;

// Query string for the reports. Add ?format=csv to download all matching rows.
public class ReportQuery : PageQuery
{
    public string? Status { get; set; }
    public string? Source { get; set; }
    public string? Stage { get; set; }
    public int? AssignedTo { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? Format { get; set; }

    public bool IsCsv => QueryFilters.IsCsv(Format);
}

public class AuditReportQuery : AuditLogQuery
{
    public AuditReportQuery()
    {
        PageSize = 10;
    }

    public string? Format { get; set; }

    public bool IsCsv => QueryFilters.IsCsv(Format);
}

// What a report method returns: either JSON data or a CSV file
public class ReportOutput
{
    public object? Data { get; set; }
    public byte[]? CsvBytes { get; set; }
    public string FileName { get; set; } = "report.csv";
}

public class CustomerReportRow
{
    public int Id { get; set; }
    public string? CustomerCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeadReportRow
{
    public int Id { get; set; }
    public string? LeadCode { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public string? AssignedToName { get; set; }
    public bool Converted { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FollowUpReportRow
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FollowUpType { get; set; } = string.Empty;
    public DateOnly FollowUpDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public string RelatedTo { get; set; } = "-";
    public string? AssignedToName { get; set; }
}

public class OpportunityReportRow
{
    public int Id { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Stage { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Probability { get; set; }
    public decimal WeightedAmount { get; set; }
    public DateOnly ExpectedCloseDate { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PipelineReportDto
{
    public List<PipelineStageRow> ByStage { get; set; } = new();
    public List<PipelineOwnerRow> ByOwner { get; set; } = new();
    public PipelineTotals Totals { get; set; } = new();
}

public class PipelineStageRow
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class PipelineOwnerRow
{
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class PipelineTotals
{
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class ConversionReportDto
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int LostLeads { get; set; }
    public double ConversionRate { get; set; }
    public int TotalOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public double WinRate { get; set; }
    public decimal WonAmount { get; set; }
    public List<SourceConversionRow> BySource { get; set; } = new();
}

public class SourceConversionRow
{
    public string Source { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Converted { get; set; }
    public double ConversionRate { get; set; }
}

public class UserActivityRow
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int TotalActions { get; set; }
    public int Logins { get; set; }
    public int FailedLogins { get; set; }
    public int Creates { get; set; }
    public int Updates { get; set; }
    public int Deletes { get; set; }
    public DateTime? LastActivity { get; set; }
}
