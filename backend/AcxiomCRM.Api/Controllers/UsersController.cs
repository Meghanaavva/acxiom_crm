using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.Controllers;

// Admin: full user administration. Manager: read-only list of his team.
[Authorize(Roles = Roles.AdminOrManager)]
public class UsersController : BaseController
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    // GET /Users?search=&role=&isActive=&page=
    public async Task<IActionResult> Index([FromQuery] UserListQuery query)
    {
        var model = new UserIndexViewModel
        {
            Result = await _users.GetListAsync(query),
            Query = query,
            IsAdmin = User.IsInRole(Roles.Admin),
        };
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create()
    {
        var model = new UserFormViewModel { Role = Roles.SalesExecutive, Active = true };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                model.IsActive = model.Active;
                if (model.Role != Roles.SalesExecutive) model.ManagerId = null;
                await _users.CreateAsync(model);
                Success("User created successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex); // e.g. duplicate email or weak password
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _users.GetByIdAsync(id);
        var model = new UserFormViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            ManagerId = user.ManagerId,
            Active = user.IsActive,
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id, UserFormViewModel model)
    {
        model.Id = id;
        // password is not changed on this form (use Reset Password instead)
        ModelState.Remove(nameof(model.Password));

        if (ModelState.IsValid)
        {
            try
            {
                var request = new UpdateUserRequest
                {
                    Name = model.Name,
                    Email = model.Email,
                    Role = model.Role,
                    ManagerId = model.Role == Roles.SalesExecutive ? model.ManagerId : null,
                    IsActive = model.Active,
                };
                await _users.UpdateAsync(id, request);
                Success("User updated successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex);
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SetStatus(int id, bool isActive)
    {
        try
        {
            await _users.SetStatusAsync(id, isActive);
            Success(isActive ? "User activated." : "User deactivated.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Unlock(int id)
    {
        try
        {
            await _users.UnlockAsync(id);
            Success("User account unlocked.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            Error(firstError ?? "Please enter a valid password.");
            return RedirectToAction(nameof(Index));
        }
        try
        {
            await _users.ResetPasswordAsync(id, model.NewPassword!);
            Success("Password reset successfully. The account is also unlocked.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            // Identity password policy messages come back as field errors
            Error(ex.Errors?.FirstOrDefault()?.Message ?? ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task FillDropdowns(UserFormViewModel model)
    {
        model.RoleOptions = Roles.All.Select(r => new SelectListItem(Ui.RoleLabel(r), r)).ToList();
        model.Managers = (await _users.GetManagersAsync())
            .Select(m => new SelectListItem(m.Name, m.Id.ToString()))
            .ToList();
    }
}
