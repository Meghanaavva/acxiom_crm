using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.ViewModels;

public class ActivityIndexViewModel
{
    public PagedResult<ActivityDto> Result { get; set; } = new();
    public ActivityListQuery Query { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}

// Create / Edit form. ActivityDate in this form is India local time (datetime-local input),
// the controller converts it to UTC before calling the service.
public class ActivityFormViewModel : ActivityRequest
{
    public int? Id { get; set; }
    public List<SelectListItem> Types { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Leads { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public PagedResult<CustomerDto> Customers { get; set; } = new();
    public PagedResult<LeadDto> Leads { get; set; } = new();
    public PagedResult<OpportunityDto> Opportunities { get; set; } = new();

    public int Total => Customers.Pagination.Total + Leads.Pagination.Total + Opportunities.Pagination.Total;
}
