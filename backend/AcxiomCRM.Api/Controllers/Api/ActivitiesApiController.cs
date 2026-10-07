using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/activities")]
[Authorize]
public class ActivitiesApiController : ControllerBase
{
    private readonly IActivityService _activities;

    public ActivitiesApiController(IActivityService activities)
    {
        _activities = activities;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ActivityDto>>> GetAll([FromQuery] ActivityListQuery query) =>
        Ok(await _activities.GetListAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActivityDto>> Get(int id) => Ok(await _activities.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<ActivityDto>> Create(ActivityRequest request)
    {
        var activity = await _activities.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = activity.Id }, activity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ActivityDto>> Update(int id, ActivityRequest request) =>
        Ok(await _activities.UpdateAsync(id, request));

    // Admin / Manager can delete within their scope, sales executives only their own (scope check in the service)
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MessageResponse>> Delete(int id)
    {
        await _activities.DeleteAsync(id);
        return Ok(new MessageResponse("Activity deleted successfully."));
    }
}
