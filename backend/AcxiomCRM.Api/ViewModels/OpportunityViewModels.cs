using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.ViewModels;

public class OpportunityIndexViewModel
{
    public PagedResult<OpportunityDto> Result { get; set; } = new();
    public OpportunityListQuery Query { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
    public List<SelectListItem> Customers { get; set; } = new();
}

// Create / Edit form - inherits the validated request (same DataAnnotations on client and server)
public class OpportunityFormViewModel : OpportunityRequest
{
    public int? Id { get; set; }
    public List<SelectListItem> Users { get; set; } = new();
    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Leads { get; set; } = new();
    public List<SelectListItem> Stages { get; set; } = new();
    public List<SelectListItem> Sources { get; set; } = new();
}

public class OpportunityDetailsViewModel
{
    public OpportunityDto Opportunity { get; set; } = new();
}
