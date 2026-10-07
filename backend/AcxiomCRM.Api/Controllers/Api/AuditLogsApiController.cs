using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

// Read only - audit logs can never be edited or deleted through the API
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = Roles.AdminOrManager)]
public class AuditLogsApiController : ControllerBase
{
    private readonly IAuditLogService _auditLogs;

    public AuditLogsApiController(IAuditLogService auditLogs)
    {
        _auditLogs = auditLogs;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll([FromQuery] AuditLogQuery query) =>
        Ok(await _auditLogs.GetListAsync(query));

    [HttpGet("filters")]
    public async Task<ActionResult<AuditFiltersDto>> Filters() => Ok(await _auditLogs.GetFiltersAsync());
}
