using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using AcxiomCRM.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers;

[Authorize]
public class CustomersController : BaseController
{
    private readonly ICustomerService _customers;
    private readonly IUserService _users;

    public CustomersController(ICustomerService customers, IUserService users)
    {
        _customers = customers;
        _users = users;
    }

    // GET /Customers?search=&status=&assignedTo=&page=&sortBy=&sortOrder=
    public async Task<IActionResult> Index([FromQuery] CustomerListQuery query)
    {
        var model = new CustomerIndexViewModel
        {
            Result = await _customers.GetListAsync(query),
            Query = query,
            Users = ToSelectList(await _users.GetLookupAsync()),
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = new CustomerDetailsViewModel
        {
            Customer = await _customers.GetByIdAsync(id),
            History = await _customers.GetHistoryAsync(id),
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CustomerFormViewModel { Status = "Active" };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var customer = await _customers.CreateAsync(model);
                Success("Customer created successfully.");
                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex); // e.g. duplicate email / phone
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customers.GetByIdAsync(id);
        var model = new CustomerFormViewModel
        {
            Id = customer.Id,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            Notes = customer.Notes,
            AssignedTo = customer.AssignedTo,
        };
        await FillDropdowns(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await _customers.UpdateAsync(id, model);
                Success("Customer updated successfully.");
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (AppException ex) when (ex.StatusCode is 400 or 409)
            {
                AddErrors(ex);
            }
        }
        await FillDropdowns(model);
        return View("Form", model);
    }

    // Sales executives cannot delete customers
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _customers.DeleteAsync(id);
            Success("Customer deleted successfully.");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            Error(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task FillDropdowns(CustomerFormViewModel model)
    {
        model.Users = ToSelectList(await _users.GetLookupAsync());
        model.Statuses = ToSelectList(CustomerStatuses.All);
    }
}
