using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.Controllers;

// Common helpers for the MVC (Razor view) controllers
public abstract class BaseController : Controller
{
    // Messages shown as toast notifications on the next page (see _Layout)
    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;

    /// <summary>
    /// Copies validation / business rule errors from the service layer into ModelState,
    /// so they are shown next to the related input (server-side validation).
    /// Field names come in camelCase ("expectedCloseDate") and are converted to property names.
    /// </summary>
    protected void AddErrors(AppException ex, string prefix = "")
    {
        if (ex.Errors == null || ex.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return;
        }
        foreach (var error in ex.Errors)
        {
            var key = error.Field == "general" ? string.Empty : prefix + char.ToUpper(error.Field[0]) + error.Field[1..];
            ModelState.AddModelError(key, error.Message);
        }
    }

    // Dropdown helpers
    protected static List<SelectListItem> ToSelectList(IEnumerable<string> values, string? selected = null) =>
        values.Select(v => new SelectListItem(v, v, v == selected)).ToList();

    protected static List<SelectListItem> ToSelectList(IEnumerable<UserLookupDto> users) =>
        users.Select(u => new SelectListItem(u.Name, u.Id.ToString())).ToList();
}
