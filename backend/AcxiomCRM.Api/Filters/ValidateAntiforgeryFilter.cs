using AcxiomCRM.Api.Dtos;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AcxiomCRM.Api.Filters;

/// <summary>
/// Anti-forgery (CSRF) protection for every state changing request (POST/PUT/PATCH/DELETE).
/// The React app reads the XSRF-TOKEN cookie and sends it back in the X-XSRF-TOKEN header.
/// A malicious site cannot read our cookie, so it cannot send a valid header.
/// Same idea as [ValidateAntiForgeryToken] on MVC forms.
/// </summary>
public class ValidateAntiforgeryFilter : IAsyncAuthorizationFilter
{
    private static readonly string[] SafeMethods = { "GET", "HEAD", "OPTIONS", "TRACE" };
    private readonly IAntiforgery _antiforgery;

    public ValidateAntiforgeryFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (SafeMethods.Contains(context.HttpContext.Request.Method)) return;

        // Only for REST API controllers - MVC actions use [ValidateAntiForgeryToken]
        var isApi = context.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any();
        if (!isApi) return;

        if (!await _antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            context.Result = new BadRequestObjectResult(
                new ErrorResponse("Invalid or missing anti-forgery token. Please refresh the page and try again."));
        }
    }
}
