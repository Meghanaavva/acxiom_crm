using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.ViewModels;

public class LeadIndexViewModel
{
    public PagedResult<LeadDto> Result { get; set; } = new();
    public LeadListQuery Query { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}

// Create / Edit form - inherits the validated request (same DataAnnotations on client and server)
public class LeadFormViewModel : LeadRequest
{
    public int? Id { get; set; }
    public string? CurrentStatus { get; set; }
    public List<SelectListItem> Users { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    public List<SelectListItem> Sources { get; set; } = new();
    public List<SelectListItem> Priorities { get; set; } = new();
}

public class LeadDetailsViewModel
{
    public LeadDto Lead { get; set; } = new();

    // Status buttons allowed from the current status (Converted only through the convert page)
    public string[] NextStatuses { get; set; } = Array.Empty<string>();
}

// Convert a qualified lead into a customer (+ optional opportunity)
public class LeadConvertViewModel : ConvertLeadRequest
{
    public LeadDto Lead { get; set; } = new();

    // Stage used by the client rules (converted opportunities start in Qualification)
    public string Stage { get; set; } = OpportunityStages.Qualification;
}
