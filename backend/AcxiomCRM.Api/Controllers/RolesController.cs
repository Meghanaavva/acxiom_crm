using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

// Roles and the permission matrix (read only) - Admin only
[Authorize(Roles = Roles.Admin)]
public class RolesController : BaseController
{
    private readonly IUserService _users;

    public RolesController(IUserService users)
    {
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var model = new RolesViewModel
        {
            Roles = await _users.GetRolesAsync(),
            Permissions = PermissionMatrix.Get(),
        };
        return View(model);
    }
}
