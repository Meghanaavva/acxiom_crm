using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.Controllers;

[Authorize]
public class FollowUpsController : BaseController
{
    private readonly IFollowUpService _followUps;
    private readonly ICustomerService _customers;
    private readonly ILeadService _leads;
    private readonly IOpportunityService _opportunities;
    private readonly IUserService _users;

    public FollowUpsController(IFollowUpService followUps, ICustomerService customers, ILeadService leads,
        IOpportunityService opportunities, IUserService users)
    {
        _followUps = followUps;
        _customers = customers;
        _leads = leads;
        _opportunities = opportunities;
        _users = users;
    }

    // GET /FollowUps?view=today|upcoming|overdue&status=&assignedTo=&customerId=&leadId=&dateFrom=&dateTo=
    public async Task<IActionResult> Index([FromQuery] FollowUpListQuery query)
    {
        var model = new FollowUpIndexViewModel
        {
            Result = await _followUps.GetListAsync(query),
            Query = query,
            Users = ToSelectList(await _users.GetLookupAsync()),
            Customers = await CustomerOptions(),
            Leads = await LeadOptions(),
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
    {
        var model = new FollowUpFormViewModel
        {
            FollowUpType = "Call",
            FollowUpDate = DateHelper.Today(),
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _followUps.CreateAsync(model);
                Success("Follow-up scheduled successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex); // e.g. date in the past, no related record
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var followUp = await _followUps.GetByIdAsync(id);
        if (followUp.Status != FollowUpStatuses.Planned)
        {
            Error("Only planned follow-ups can be edited.");
            return RedirectToAction(nameof(Index));
        }

        var model = new FollowUpFormViewModel
        {
            Id = followUp.Id,
            Subject = followUp.Subject,
            FollowUpType = followUp.FollowUpType,
            FollowUpDate = followUp.FollowUpDate,
            Remarks = followUp.Remarks,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            AssignedTo = followUp.AssignedTo,
        };
        await FillDropdowns(model, followUp);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpFormViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await _followUps.UpdateAsync(id, model);
                Success("Follow-up updated successfully.");
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks, string? returnUrl)
    {
        if (remarks is { Length: > 1000 })
        {
            Error("Remarks cannot be more than 1000 characters.");
            return BackToList(returnUrl);
        }
        await RunAction(() => _followUps.CompleteAsync(id, remarks), "Follow-up marked as completed.");
        return BackToList(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Missed(int id, string? returnUrl)
    {
        await RunAction(() => _followUps.MarkMissedAsync(id), "Follow-up marked as missed.");
        return BackToList(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl)
    {
        await RunAction(() => _followUps.CancelAsync(id), "Follow-up cancelled.");
        return BackToList(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, RescheduleFollowUpRequest model, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            // show the first validation message as a toast
            var message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            Error(string.IsNullOrEmpty(message) ? "Please enter a valid follow-up date." : message);
            return BackToList(returnUrl);
        }
        await RunAction(() => _followUps.RescheduleAsync(id, model), "Follow-up rescheduled.");
        return BackToList(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        await RunAction(async () => { await _followUps.DeleteAsync(id); return true; }, "Follow-up deleted successfully.");
        return BackToList(returnUrl);
    }

    // Runs a status action and shows the result as a toast. 403 / 404 go to the error pages.
    private async Task RunAction<T>(Func<Task<T>> action, string successMessage)
    {
        try
        {
            await action();
            Success(successMessage);
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            var message = ex.Errors?.FirstOrDefault()?.Message ?? ex.Message;
            Error(message);
        }
    }

    // Go back to the same list page (keeps filters). Only local urls are allowed.
    private IActionResult BackToList(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> CustomerOptions()
    {
        var customers = await _customers.GetListAsync(new CustomerListQuery { PageSize = 100, SortBy = "customerName", SortOrder = "asc" });
        return customers.Data.Select(c => new SelectListItem(c.CustomerName, c.Id.ToString())).ToList();
    }

    private async Task<List<SelectListItem>> LeadOptions()
    {
        var leads = await _leads.GetListAsync(new LeadListQuery { PageSize = 100, SortBy = "leadName", SortOrder = "asc" });
        return leads.Data.Select(l => new SelectListItem(l.LeadName, l.Id.ToString())).ToList();
    }

    private async Task FillDropdowns(FollowUpFormViewModel model, FollowUpDto? current = null)
    {
        model.Types = ToSelectList(FollowUpTypes.All);
        model.Customers = await CustomerOptions();
        model.Leads = await LeadOptions();
        model.Users = ToSelectList(await _users.GetLookupAsync());

        // only open opportunities can get new follow-ups
        var opportunities = await _opportunities.GetListAsync(new OpportunityListQuery { PageSize = 100, Status = "Open", SortBy = "opportunityName", SortOrder = "asc" });
        model.Opportunities = opportunities.Data.Select(o => new SelectListItem($"{o.OpportunityName} ({o.CustomerName})", o.Id.ToString())).ToList();

        // when editing, keep the currently linked records in the lists even if they are not in the first 100
        if (current?.CustomerId != null && model.Customers.All(c => c.Value != current.CustomerId.ToString()))
            model.Customers.Insert(0, new SelectListItem(current.CustomerName, current.CustomerId.ToString()));
        if (current?.LeadId != null && model.Leads.All(l => l.Value != current.LeadId.ToString()))
            model.Leads.Insert(0, new SelectListItem(current.LeadName, current.LeadId.ToString()));
        if (current?.OpportunityId != null && model.Opportunities.All(o => o.Value != current.OpportunityId.ToString()))
            model.Opportunities.Insert(0, new SelectListItem(current.OpportunityName, current.OpportunityId.ToString()));
    }
}
