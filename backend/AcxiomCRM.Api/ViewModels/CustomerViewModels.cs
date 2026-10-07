using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.ViewModels;

public class CustomerIndexViewModel
{
    public PagedResult<CustomerDto> Result { get; set; } = new();
    public CustomerListQuery Query { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}

// Create / Edit form. Inherits the validated request so the same DataAnnotations
// drive the client-side (unobtrusive) and server-side validation.
public class CustomerFormViewModel : CustomerRequest
{
    public int? Id { get; set; }
    public List<SelectListItem> Users { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
}

public class CustomerDetailsViewModel
{
    public CustomerDto Customer { get; set; } = new();
    public CustomerHistoryDto History { get; set; } = new();
}
