using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardApiController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    // GET api/dashboard?range=all|today|week|month|custom&from=YYYY-MM-DD&to=YYYY-MM-DD
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] DashboardQuery query) =>
        Ok(await _dashboard.GetDashboardAsync(query));
}
