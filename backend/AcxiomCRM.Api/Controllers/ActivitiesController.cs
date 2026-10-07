using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.Controllers;

[Authorize]
public class ActivitiesController : BaseController
{
    private readonly IActivityService _activities;
    private readonly ICustomerService _customers;
    private readonly ILeadService _leads;
    private readonly IUserService _users;

    public ActivitiesController(IActivityService activities, ICustomerService customers, ILeadService leads, IUserService users)
    {
        _activities = activities;
        _customers = customers;
        _leads = leads;
        _users = users;
    }

    // GET /Activities?search=&activityType=&status=&assignedTo=&dateFrom=&dateTo=
    public async Task<IActionResult> Index([FromQuery] ActivityListQuery query)
    {
        var model = new ActivityIndexViewModel
        {
            Result = await _activities.GetListAsync(query),
            Query = query,
            Users = ToSelectList(await _users.GetLookupAsync()),
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        var now = DateHelper.NowLocal();
        var model = new ActivityFormViewModel
        {
            ActivityType = "Call",
            Status = "Planned",
            // datetime-local works in minutes
            ActivityDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
            CustomerId = customerId,
            LeadId = leadId,
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _activities.CreateAsync(ToRequest(model));
                Success("Activity logged successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex);
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var activity = await _activities.GetByIdAsync(id);
        var local = DateHelper.ToLocal(activity.ActivityDate);
        var model = new ActivityFormViewModel
        {
            Id = activity.Id,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0),
            Status = activity.Status,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            AssignedTo = activity.AssignedTo,
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityFormViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await _activities.UpdateAsync(id, ToRequest(model));
                Success("Activity updated successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex);
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    // Admin / Manager or the owner (the service checks the record scope)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _activities.DeleteAsync(id);
            Success("Activity deleted successfully.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    // The form sends India local time, the service stores UTC
    private static ActivityRequest ToRequest(ActivityFormViewModel model) => new()
    {
        ActivityType = model.ActivityType,
        Subject = model.Subject,
        Description = model.Description,
        ActivityDate = model.ActivityDate.HasValue ? LocalToUtc(model.ActivityDate.Value) : null,
        Status = model.Status,
        CustomerId = model.CustomerId,
        LeadId = model.LeadId,
        AssignedTo = model.AssignedTo,
    };

    private static DateTime LocalToUtc(DateTime local)
    {
        // difference between India time and UTC (+05:30), rounded to minutes
        var utcNow = DateTime.UtcNow;
        var offsetMinutes = Math.Round((DateHelper.ToLocal(utcNow) - utcNow).TotalMinutes);
        var utc = DateTime.SpecifyKind(local, DateTimeKind.Unspecified).AddMinutes(-offsetMinutes);
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
    }

    private async Task FillDropdowns(ActivityFormViewModel model)
    {
        model.Types = ToSelectList(ActivityTypes.All);
        model.Statuses = ToSelectList(ActivityStatuses.All);
        model.Users = ToSelectList(await _users.GetLookupAsync());

        var customers = await _customers.GetListAsync(new CustomerListQuery { PageSize = 100, SortBy = "customerName", SortOrder = "asc" });
        model.Customers = customers.Data.Select(c => new SelectListItem(c.CustomerName, c.Id.ToString())).ToList();

        var leads = await _leads.GetListAsync(new LeadListQuery { PageSize = 100, SortBy = "leadName", SortOrder = "asc" });
        model.Leads = leads.Data.Select(l => new SelectListItem(l.LeadName, l.Id.ToString())).ToList();
    }
}
