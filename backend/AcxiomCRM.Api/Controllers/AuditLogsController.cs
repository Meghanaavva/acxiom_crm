using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

// Read-only audit log. Admin sees everything, Manager only his team (scoped in AuditLogService).
[Authorize(Roles = Roles.AdminOrManager)]
public class AuditLogsController : BaseController
{
    private readonly IAuditLogService _auditLogs;
    private readonly IUserService _users;

    public AuditLogsController(IAuditLogService auditLogs, IUserService users)
    {
        _auditLogs = auditLogs;
        _users = users;
    }

    // GET /AuditLogs?userId=&entityName=&action=&result=&dateFrom=&dateTo=&search=&page=
    public async Task<IActionResult> Index([FromQuery] AuditLogQuery query)
    {
        PagedResult<AuditLogDto> result;
        try
        {
            result = await _auditLogs.GetListAsync(query);
        }
        catch (AppException ex) when (ex.StatusCode == 400)
        {
            // e.g. invalid date range - show the message and an empty list
            Error(ex.Errors?.FirstOrDefault()?.Message ?? ex.Message);
            result = new PagedResult<AuditLogDto> { Pagination = new PaginationInfo { Page = 1, PageSize = query.PageSize, TotalPages = 1 } };
        }

        var model = new AuditLogIndexViewModel
        {
            Result = result,
            Query = query,
            Users = ToSelectList(await _users.GetLookupAsync()),
            Filters = await _auditLogs.GetFiltersAsync(),
        };
        return View(model);
    }
}
