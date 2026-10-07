using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

// Global search from the header: customers, leads and opportunities (each scoped by the user's role)
[Authorize]
public class SearchController : BaseController
{
    private readonly ICustomerService _customers;
    private readonly ILeadService _leads;
    private readonly IOpportunityService _opportunities;

    public SearchController(ICustomerService customers, ILeadService leads, IOpportunityService opportunities)
    {
        _customers = customers;
        _leads = leads;
        _opportunities = opportunities;
    }

    // GET /Search?q=gupta
    public async Task<IActionResult> Index(string? q)
    {
        var model = new SearchViewModel { Query = q?.Trim() ?? string.Empty };
        if (model.Query.Length > 0)
        {
            model.Customers = await _customers.GetListAsync(new CustomerListQuery { Search = model.Query, PageSize = 10 });
            model.Leads = await _leads.GetListAsync(new LeadListQuery { Search = model.Query, PageSize = 10 });
            model.Opportunities = await _opportunities.GetListAsync(new OpportunityListQuery { Search = model.Query, PageSize = 10 });
        }
        return View(model);
    }
}
