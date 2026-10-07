using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leads;

    public LeadsApiController(ILeadService leads)
    {
        _leads = leads;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadDto>>> GetAll([FromQuery] LeadListQuery query) =>
        Ok(await _leads.GetListAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeadDto>> Get(int id) => Ok(await _leads.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create(LeadRequest request)
    {
        var lead = await _leads.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = lead.Id }, lead);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LeadDto>> Update(int id, LeadRequest request) =>
        Ok(await _leads.UpdateAsync(id, request));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<LeadDto>> ChangeStatus(int id, LeadStatusRequest request) =>
        Ok(await _leads.ChangeStatusAsync(id, request.Status!));

    // Qualified lead -> customer (+ optional opportunity)
    [HttpPost("{id:int}/convert")]
    public async Task<ActionResult<ConvertLeadResponse>> Convert(int id, ConvertLeadRequest request) =>
        Ok(await _leads.ConvertAsync(id, request));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<ActionResult<MessageResponse>> Delete(int id)
    {
        await _leads.DeleteAsync(id);
        return Ok(new MessageResponse("Lead deleted successfully."));
    }
}
