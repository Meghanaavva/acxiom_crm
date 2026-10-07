using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = Roles.Admin)]
public class RolesApiController : ControllerBase
{
    private static readonly List<string> Modules = new()
    {
        "Dashboard", "Customers", "Leads", "Follow-Ups", "Opportunities", "Activities",
        "User Management", "Role Management", "Audit Log", "REST API", "Reports",
    };

    // Permission matrix from section 7.1 of the project document.
    // The real checks are done by [Authorize(Roles = ...)] and ScopeService.
    private static readonly Dictionary<string, Dictionary<string, string>> Matrix = new()
    {
        [Roles.Admin] = new()
        {
            ["Dashboard"] = "Full",
            ["Customers"] = "Full",
            ["Leads"] = "Full",
            ["Follow-Ups"] = "Full",
            ["Opportunities"] = "Full",
            ["Activities"] = "Full",
            ["User Management"] = "Full",
            ["Role Management"] = "Full",
            ["Audit Log"] = "Full",
            ["REST API"] = "Authorized endpoints",
            ["Reports"] = "All",
        },
        [Roles.Manager] = new()
        {
            ["Dashboard"] = "Team",
            ["Customers"] = "Team",
            ["Leads"] = "Team",
            ["Follow-Ups"] = "Team",
            ["Opportunities"] = "Team",
            ["Activities"] = "Team",
            ["User Management"] = "View team only",
            ["Role Management"] = "No",
            ["Audit Log"] = "Limited (team)",
            ["REST API"] = "Authorized endpoints",
            ["Reports"] = "Management / team reports",
        },
        [Roles.SalesExecutive] = new()
        {
            ["Dashboard"] = "Own/Assigned",
            ["Customers"] = "Own/Assigned",
            ["Leads"] = "Own/Assigned",
            ["Follow-Ups"] = "Own/Assigned",
            ["Opportunities"] = "Own/Assigned",
            ["Activities"] = "Own/Assigned",
            ["User Management"] = "No",
            ["Role Management"] = "No",
            ["Audit Log"] = "No",
            ["REST API"] = "Authorized endpoints",
            ["Reports"] = "Own/assigned reports",
        },
    };

    private readonly IUserService _users;

    public RolesApiController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll() => Ok(await _users.GetRolesAsync());

    [HttpGet("permissions")]
    public ActionResult<PermissionMatrixDto> Permissions() =>
        Ok(new PermissionMatrixDto { Modules = Modules, Matrix = Matrix });
}
