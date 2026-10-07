using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

// All reports are scoped by role inside the service. Add ?format=csv to export.
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsApiController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers([FromQuery] ReportQuery query) => Send(await _reports.CustomerReportAsync(query));

    [HttpGet("leads")]
    public async Task<IActionResult> Leads([FromQuery] ReportQuery query) => Send(await _reports.LeadReportAsync(query));

    [HttpGet("followups")]
    public async Task<IActionResult> FollowUps([FromQuery] ReportQuery query) => Send(await _reports.FollowUpReportAsync(query));

    [HttpGet("opportunities")]
    public async Task<IActionResult> Opportunities([FromQuery] ReportQuery query) => Send(await _reports.OpportunityReportAsync(query));

    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline([FromQuery] ReportQuery query) => Send(await _reports.PipelineReportAsync(query));

    [HttpGet("conversion")]
    public async Task<IActionResult> Conversion([FromQuery] ReportQuery query) => Send(await _reports.ConversionReportAsync(query));

    [HttpGet("user-activity")]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> UserActivity([FromQuery] ReportQuery query) => Send(await _reports.UserActivityReportAsync(query));

    [HttpGet("audit")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Audit([FromQuery] AuditReportQuery query) => Send(await _reports.AuditReportAsync(query));

    // JSON for the screen, file download for CSV
    private IActionResult Send(ReportOutput output) =>
        output.CsvBytes != null ? File(output.CsvBytes, "text/csv", output.FileName) : Ok(output.Data);
}
