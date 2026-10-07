using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

[AllowAnonymous]
public class HomeController : BaseController
{
    // Friendly error page - technical details are only written to the server log
    public IActionResult Error() => View();

    [Route("Home/StatusCode/{code:int}")]
    public new IActionResult StatusCode(int code)
    {
        // API calls keep their JSON responses
        if (HttpContext.Request.Path.StartsWithSegments("/api")) return new EmptyResult();
        ViewData["Code"] = code;
        return View("StatusCode");
    }
}
