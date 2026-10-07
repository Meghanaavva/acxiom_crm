using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.Controllers;

[Authorize]
public class OpportunitiesController : BaseController
{
    private readonly IOpportunityService _opportunities;
    private readonly ICustomerService _customers;
    private readonly ILeadService _leads;
    private readonly IUserService _users;

    public OpportunitiesController(IOpportunityService opportunities, ICustomerService customers, ILeadService leads, IUserService users)
    {
        _opportunities = opportunities;
        _customers = customers;
        _leads = leads;
        _users = users;
    }

    // GET /Opportunities?search=&customerId=&stage=&status=&assignedTo=&page=&sortBy=&sortOrder=
    public async Task<IActionResult> Index([FromQuery] OpportunityListQuery query)
    {
        var model = new OpportunityIndexViewModel
        {
            Result = await _opportunities.GetListAsync(query),
            Query = query,
            Users = ToSelectList(await _users.GetLookupAsync()),
            Customers = await CustomerOptions(),
        };
        return View(model);
    }

    // Kanban style board: one column per stage
    public async Task<IActionResult> Pipeline() => View(await _opportunities.GetPipelineAsync());

    public async Task<IActionResult> Details(int id)
    {
        var model = new OpportunityDetailsViewModel { Opportunity = await _opportunities.GetByIdAsync(id) };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        var model = new OpportunityFormViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            Stage = OpportunityStages.Qualification,
            Probability = 20,
            ExpectedCloseDate = DateHelper.Today().AddMonths(1),
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var opportunity = await _opportunities.CreateAsync(model);
                Success("Opportunity created successfully.");
                return RedirectToAction(nameof(Details), new { id = opportunity.Id });
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex); // amount / probability / close date business rules
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var o = await _opportunities.GetByIdAsync(id);
        if (o.Status != "Open")
        {
            Error($"A {o.Status} opportunity cannot be edited.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new OpportunityFormViewModel
        {
            Id = o.Id,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Source = o.Source,
            Notes = o.Notes,
            AssignedTo = o.AssignedTo,
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityFormViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await _opportunities.UpdateAsync(id, model);
                Success("Opportunity updated successfully.");
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex);
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    // Stage buttons on the details page and the pipeline board
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStage(int id, string stage, string? returnTo)
    {
        try
        {
            await _opportunities.ChangeStageAsync(id, stage);
            Success($"Opportunity moved to {stage}.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Errors?.FirstOrDefault()?.Message ?? ex.Message);
        }
        return returnTo == "pipeline" ? RedirectToAction(nameof(Pipeline)) : RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _opportunities.DeleteAsync(id);
            Success("Opportunity deleted successfully.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(Index));
    }

    // Customers the user can see (max 100, sorted by name)
    private async Task<List<SelectListItem>> CustomerOptions()
    {
        var customers = await _customers.GetListAsync(new CustomerListQuery { PageSize = 100, SortBy = "customerName", SortOrder = "asc" });
        return customers.Data.Select(c => new SelectListItem(c.CustomerName, c.Id.ToString())).ToList();
    }

    private async Task FillDropdowns(OpportunityFormViewModel model)
    {
        model.Users = ToSelectList(await _users.GetLookupAsync());
        model.Customers = await CustomerOptions();
        var leads = await _leads.GetListAsync(new LeadListQuery { PageSize = 100, SortBy = "leadName", SortOrder = "asc" });
        model.Leads = leads.Data.Select(l => new SelectListItem(l.LeadName, l.Id.ToString())).ToList();
        model.Stages = ToSelectList(OpportunityStages.Open);
        model.Sources = ToSelectList(LeadSources.All);
    }
}
