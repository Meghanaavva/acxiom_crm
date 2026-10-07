using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunities;

    public OpportunitiesApiController(IOpportunityService opportunities)
    {
        _opportunities = opportunities;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OpportunityDto>>> GetAll([FromQuery] OpportunityListQuery query) =>
        Ok(await _opportunities.GetListAsync(query));

    // Grouped by stage for the pipeline board
    [HttpGet("pipeline")]
    public async Task<ActionResult<PipelineDto>> Pipeline() => Ok(await _opportunities.GetPipelineAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityDto>> Get(int id) => Ok(await _opportunities.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> Create(OpportunityRequest request)
    {
        var opportunity = await _opportunities.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = opportunity.Id }, opportunity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OpportunityDto>> Update(int id, OpportunityRequest request) =>
        Ok(await _opportunities.UpdateAsync(id, request));

    [HttpPatch("{id:int}/stage")]
    public async Task<ActionResult<OpportunityDto>> ChangeStage(int id, OpportunityStageRequest request) =>
        Ok(await _opportunities.ChangeStageAsync(id, request.Stage!));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<ActionResult<MessageResponse>> Delete(int id)
    {
        await _opportunities.DeleteAsync(id);
        return Ok(new MessageResponse("Opportunity deleted successfully."));
    }
}
