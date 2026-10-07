using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersApiController : ControllerBase
{
    private readonly IUserService _users;

    public UsersApiController(IUserService users)
    {
        _users = users;
    }

    // Admin sees everyone, Manager sees only his team
    [HttpGet]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] UserListQuery query) =>
        Ok(await _users.GetListAsync(query));

    // Users for "Assigned To" dropdowns - available to every role
    [HttpGet("lookup")]
    public async Task<ActionResult<List<UserLookupDto>>> Lookup() => Ok(await _users.GetLookupAsync());

    [HttpGet("managers")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<ManagerLookupDto>>> Managers() => Ok(await _users.GetManagersAsync());

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> Get(int id) => Ok(await _users.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = await _users.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request) =>
        Ok(await _users.UpdateAsync(id, request));

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> SetStatus(int id, UserStatusRequest request) =>
        Ok(await _users.SetStatusAsync(id, request.IsActive!.Value));

    [HttpPost("{id:int}/reset-password")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MessageResponse>> ResetPassword(int id, ResetPasswordRequest request)
    {
        await _users.ResetPasswordAsync(id, request.NewPassword!);
        return Ok(new MessageResponse("Password reset successfully."));
    }

    [HttpPost("{id:int}/unlock")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> Unlock(int id) => Ok(await _users.UnlockAsync(id));
}
