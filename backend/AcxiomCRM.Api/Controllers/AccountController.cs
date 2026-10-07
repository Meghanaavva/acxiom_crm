using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AcxiomCRM.Api.Controllers;

// Login / Register / Logout / Change password pages (ASP.NET Core Identity via AuthService)
public class AccountController : BaseController
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _auth.LoginAsync(model);
        }
        catch (AppException ex)
        {
            // wrong password / locked / inactive
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        // only redirect to local urls (open redirect protection)
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _auth.RegisterAsync(model);
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            AddErrors(ex);
            return View(model);
        }
        Success("Registration successful. Please login.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _auth.LogoutAsync();
        Success("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _auth.ChangePasswordAsync(model);
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            AddErrors(ex);
            return View(model);
        }
        Success("Password changed successfully. Please login again.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
