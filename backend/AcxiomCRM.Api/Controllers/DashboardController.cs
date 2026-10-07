using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboard;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IDashboardService dashboard, ICurrentUserService currentUser)
    {
        _dashboard = dashboard;
        _currentUser = currentUser;
    }

    // GET / or /Dashboard?range=all|today|week|month|custom&from=YYYY-MM-DD&to=YYYY-MM-DD
    public async Task<IActionResult> Index(string? range, string? from, string? to)
    {
        var user = await _currentUser.GetAsync();
        var model = new DashboardViewModel
        {
            Range = string.IsNullOrEmpty(range) ? "all" : range,
            From = from,
            To = to,
            UserName = user.Name,
            Role = user.Role,
        };

        try
        {
            model.Data = await _dashboard.GetDashboardAsync(new DashboardQuery { Range = model.Range, From = from, To = to });
        }
        catch (AppException ex) when (ex.StatusCode == 400)
        {
            // invalid custom range -> show the message and fall back to all time
            model.ErrorMessage = ex.Message;
            model.Range = "all";
            model.Data = await _dashboard.GetDashboardAsync(new DashboardQuery { Range = "all" });
        }
        return View(model);
    }
}
