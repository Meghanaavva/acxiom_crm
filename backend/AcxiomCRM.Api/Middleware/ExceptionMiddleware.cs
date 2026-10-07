using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Middleware;

// Converts every exception into a clean JSON response.
// Stack traces and database errors are logged on the server but never sent to the client.
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex) when (!IsApi(context))
        {
            // MVC pages: show the access denied / not found page instead of JSON
            var target = ex.StatusCode switch
            {
                401 => "/Account/Login",
                403 => "/Account/AccessDenied",
                404 => "/Home/StatusCode/404",
                _ => "/Home/Error",
            };
            context.Response.Redirect(target);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, new ErrorResponse(ex.Message, ex.Errors));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
        {
            await WriteAsync(context, 409, new ErrorResponse("This record already exists."));
        }
        catch (Exception ex) when (!IsApi(context))
        {
            _logger.LogError(ex, "Unhandled error for {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.Redirect("/Home/Error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, 500, new ErrorResponse("Something went wrong. Please try again later."));
        }
    }

    private static bool IsApi(HttpContext context) => context.Request.Path.StartsWithSegments("/api");

    private static async Task WriteAsync(HttpContext context, int status, ErrorResponse body)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(body);
    }
}
