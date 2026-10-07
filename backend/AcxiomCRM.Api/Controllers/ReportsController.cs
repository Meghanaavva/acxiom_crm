using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

// Reports screen. All data is scoped by role inside ReportService.
[Authorize]
public class ReportsController : BaseController
{
    private readonly IReportService _reports;
    private readonly IUserService _users;
    private readonly IAuditLogService _auditLogs;

    public ReportsController(IReportService reports, IUserService users, IAuditLogService auditLogs)
    {
        _reports = reports;
        _users = users;
        _auditLogs = auditLogs;
    }

    // GET /Reports?type=customers&status=&assignedTo=&dateFrom=&dateTo=&page=&sortBy=&sortOrder=
    public async Task<IActionResult> Index(string? type, [FromQuery] ReportQuery query, [FromQuery] AuditReportQuery auditQuery)
    {
        type = string.IsNullOrEmpty(type) ? "customers" : type;
        if (!CanView(type)) return RedirectToAction("AccessDenied", "Account");

        // the screen always shows a page of data, CSV is only for Export
        query.Format = null;
        auditQuery.Format = null;

        var model = new ReportsViewModel
        {
            Type = type,
            Tabs = GetTabs(),
            Query = query,
            AuditQuery = auditQuery,
            Users = ToSelectList(await _users.GetLookupAsync()),
            ExportUrl = Url.Action(nameof(Export)) + Request.QueryString.Value,
        };
        if (type == "audit") model.AuditFilters = await _auditLogs.GetFiltersAsync();

        try
        {
            model.Data = (await RunReportAsync(type, query, auditQuery)).Data;
        }
        catch (AppException ex) when (ex.StatusCode == 400)
        {
            model.ErrorMessage = ex.Message; // e.g. invalid date range
        }
        return View(model);
    }

    // GET /Reports/Export?type=leads&... -> CSV file with all matching rows
    public async Task<IActionResult> Export(string? type, [FromQuery] ReportQuery query, [FromQuery] AuditReportQuery auditQuery)
    {
        type = string.IsNullOrEmpty(type) ? "customers" : type;
        if (!CanView(type)) return RedirectToAction("AccessDenied", "Account");

        query.Format = "csv";
        auditQuery.Format = "csv";
        try
        {
            var output = await RunReportAsync(type, query, auditQuery);
            if (output.CsvBytes == null) return RedirectToAction(nameof(Index), new { type });
            return File(output.CsvBytes, "text/csv", output.FileName);
        }
        catch (AppException ex) when (ex.StatusCode == 400)
        {
            Error(ex.Message);
            return RedirectToAction(nameof(Index), new { type });
        }
    }

    private Task<ReportOutput> RunReportAsync(string type, ReportQuery query, AuditReportQuery auditQuery) => type switch
    {
        "leads" => _reports.LeadReportAsync(query),
        "followups" => _reports.FollowUpReportAsync(query),
        "opportunities" => _reports.OpportunityReportAsync(query),
        "pipeline" => _reports.PipelineReportAsync(query),
        "conversion" => _reports.ConversionReportAsync(query),
        "user-activity" => _reports.UserActivityReportAsync(query),
        "audit" => _reports.AuditReportAsync(auditQuery),
        _ => _reports.CustomerReportAsync(query),
    };

    // User activity: Admin / Manager. Audit: Admin only.
    private bool CanView(string type) => type switch
    {
        "user-activity" => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager),
        "audit" => User.IsInRole(Roles.Admin),
        _ => true,
    };

    private List<ReportTab> GetTabs()
    {
        var tabs = new List<ReportTab>
        {
            new() { Type = "customers", Label = "Customers", Icon = "bi-people" },
            new() { Type = "leads", Label = "Leads", Icon = "bi-person-plus" },
            new() { Type = "followups", Label = "Follow-Ups", Icon = "bi-calendar-check" },
            new() { Type = "opportunities", Label = "Opportunities", Icon = "bi-briefcase" },
            new() { Type = "pipeline", Label = "Pipeline", Icon = "bi-kanban" },
            new() { Type = "conversion", Label = "Conversion", Icon = "bi-arrow-left-right" },
        };
        if (CanView("user-activity")) tabs.Add(new() { Type = "user-activity", Label = "User Activity", Icon = "bi-person-lines-fill" });
        if (CanView("audit")) tabs.Add(new() { Type = "audit", Label = "Audit", Icon = "bi-journal-text" });
        return tabs;
    }
}
