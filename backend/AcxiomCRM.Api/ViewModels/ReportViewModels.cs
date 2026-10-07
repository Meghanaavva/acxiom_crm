using AcxiomCRM.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.ViewModels;

public class ReportTab
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class ReportsViewModel
{
    public string Type { get; set; } = "customers";
    public List<ReportTab> Tabs { get; set; } = new();

    // Filters entered by the user (same names as the query string)
    public ReportQuery Query { get; set; } = new();
    public AuditReportQuery AuditQuery { get; set; } = new();

    // Report result - type depends on the selected tab (PagedResult<...>, PipelineReportDto, ConversionReportDto)
    public object? Data { get; set; }

    public List<SelectListItem> Users { get; set; } = new();
    public AuditFiltersDto AuditFilters { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string ExportUrl { get; set; } = string.Empty;
}
