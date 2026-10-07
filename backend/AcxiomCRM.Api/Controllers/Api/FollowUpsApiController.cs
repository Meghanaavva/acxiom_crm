using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUps;

    public FollowUpsApiController(IFollowUpService followUps)
    {
        _followUps = followUps;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<FollowUpDto>>> GetAll([FromQuery] FollowUpListQuery query) =>
        Ok(await _followUps.GetListAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FollowUpDto>> Get(int id) => Ok(await _followUps.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<FollowUpDto>> Create(FollowUpRequest request)
    {
        var followUp = await _followUps.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = followUp.Id }, followUp);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FollowUpDto>> Update(int id, FollowUpRequest request) =>
        Ok(await _followUps.UpdateAsync(id, request));

    // Body is optional: { "remarks": "..." }
    [HttpPatch("{id:int}/complete")]
    public async Task<ActionResult<FollowUpDto>> Complete(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CompleteFollowUpRequest? request) =>
        Ok(await _followUps.CompleteAsync(id, request?.Remarks));

    [HttpPatch("{id:int}/missed")]
    public async Task<ActionResult<FollowUpDto>> Missed(int id) => Ok(await _followUps.MarkMissedAsync(id));

    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<FollowUpDto>> Cancel(int id) => Ok(await _followUps.CancelAsync(id));

    [HttpPost("{id:int}/reschedule")]
    public async Task<ActionResult<FollowUpDto>> Reschedule(int id, RescheduleFollowUpRequest request) =>
        Ok(await _followUps.RescheduleAsync(id, request));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<ActionResult<MessageResponse>> Delete(int id)
    {
        await _followUps.DeleteAsync(id);
        return Ok(new MessageResponse("Follow-up deleted successfully."));
    }
}
