using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersApiController(ICustomerService customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetAll([FromQuery] CustomerListQuery query) =>
        Ok(await _customers.GetListAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Get(int id) => Ok(await _customers.GetByIdAsync(id));

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<CustomerHistoryDto>> History(int id) => Ok(await _customers.GetHistoryAsync(id));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CustomerRequest request)
    {
        var customer = await _customers.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerRequest request) =>
        Ok(await _customers.UpdateAsync(id, request));

    // Sales executives cannot delete customers
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<ActionResult<MessageResponse>> Delete(int id)
    {
        await _customers.DeleteAsync(id);
        return Ok(new MessageResponse("Customer deleted successfully."));
    }
}
