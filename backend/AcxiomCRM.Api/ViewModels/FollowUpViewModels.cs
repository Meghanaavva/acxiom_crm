using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.ViewModels;

public class FollowUpIndexViewModel
{
    public PagedResult<FollowUpDto> Result { get; set; } = new();
    public FollowUpListQuery Query { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Leads { get; set; } = new();
}

// Create / Edit form - inherits the validated request (same DataAnnotations on client and server)
public class FollowUpFormViewModel : FollowUpRequest
{
    public int? Id { get; set; }
    public List<SelectListItem> Types { get; set; } = new();
    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Leads { get; set; } = new();
    public List<SelectListItem> Opportunities { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}
