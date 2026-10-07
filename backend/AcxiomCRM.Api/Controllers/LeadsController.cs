using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

[Authorize]
public class LeadsController : BaseController
{
    private readonly ILeadService _leads;
    private readonly IUserService _users;

    public LeadsController(ILeadService leads, IUserService users)
    {
        _leads = leads;
        _users = users;
    }

    // GET /Leads?search=&status=&source=&priority=&assignedTo=&page=&sortBy=&sortOrder=
    public async Task<IActionResult> Index([FromQuery] LeadListQuery query)
    {
        var model = new LeadIndexViewModel
        {
            Result = await _leads.GetListAsync(query),
            Query = query,
            Users = ToSelectList(await _users.GetLookupAsync()),
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leads.GetByIdAsync(id);
        var model = new LeadDetailsViewModel
        {
            Lead = lead,
            NextStatuses = LeadStatuses.Transitions.GetValueOrDefault(lead.Status, Array.Empty<string>()),
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new LeadFormViewModel { Status = LeadStatuses.New, Priority = "Medium", Source = "Website", ExpectedValue = 0 };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var lead = await _leads.CreateAsync(model);
                Success("Lead created successfully.");
                return RedirectToAction(nameof(Details), new { id = lead.Id });
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex); // e.g. email and phone both missing
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leads.GetByIdAsync(id);
        if (IsClosed(lead.Status))
        {
            Error($"A {lead.Status} lead cannot be edited.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new LeadFormViewModel
        {
            Id = lead.Id,
            CurrentStatus = lead.Status,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            Notes = lead.Notes,
            AssignedTo = lead.AssignedTo,
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadFormViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await _leads.UpdateAsync(id, model);
                Success("Lead updated successfully.");
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex); // e.g. invalid status transition
            }
        }
        // status options are based on the saved status, not the posted one
        var saved = await _leads.GetByIdAsync(id);
        model.CurrentStatus = saved.Status;
        await FillDropdowns(model);
        return View("Form", model);
    }

    // Status buttons on the details page
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string status)
    {
        try
        {
            await _leads.ChangeStatusAsync(id, status);
            Success($"Lead status changed to {status}.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Errors?.FirstOrDefault()?.Message ?? ex.Message);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leads.GetByIdAsync(id);
        if (lead.Status != LeadStatuses.Qualified)
        {
            Error("Only qualified leads can be converted.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new LeadConvertViewModel
        {
            Lead = lead,
            CreateOpportunity = true,
            OpportunityName = $"{lead.CompanyName ?? lead.LeadName} - New Deal",
            Amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : null,
            Probability = 20,
            ExpectedCloseDate = DateHelper.Today().AddMonths(1),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id, LeadConvertViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var result = await _leads.ConvertAsync(id, model);
                Success(result.Opportunity != null
                    ? "Lead converted to customer and opportunity."
                    : "Lead converted to customer.");
                return RedirectToAction("Details", "Customers", new { id = result.Customer.Id });
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex);
            }
        }
        model.Lead = await _leads.GetByIdAsync(id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _leads.DeleteAsync(id);
            Success("Lead deleted successfully.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(Index));
    }

    private static bool IsClosed(string status) => status == LeadStatuses.Converted || status == LeadStatuses.Lost;

    private async Task FillDropdowns(LeadFormViewModel model)
    {
        model.Users = ToSelectList(await _users.GetLookupAsync());
        model.Sources = ToSelectList(LeadSources.All);
        model.Priorities = ToSelectList(LeadPriorities.All);

        // New lead: New / Contacted / Qualified. Existing lead: current status + allowed next statuses.
        var statuses = model.CurrentStatus == null
            ? LeadStatuses.AllowedOnCreate
            : new[] { model.CurrentStatus }.Concat(LeadStatuses.Transitions.GetValueOrDefault(model.CurrentStatus, Array.Empty<string>()));
        model.Statuses = ToSelectList(statuses);
    }
}
