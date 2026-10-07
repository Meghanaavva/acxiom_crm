using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AcxiomCRM.Api.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAntiforgery _antiforgery;

    public AuthApiController(IAuthService auth, IAntiforgery antiforgery)
    {
        _auth = auth;
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Issues the anti-forgery token. The React app calls this on start up and after login/logout.
    /// The token is put in a readable XSRF-TOKEN cookie and must be sent back in the X-XSRF-TOKEN header.
    /// </summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false, // JavaScript must read it
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Path = "/",
        });
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _auth.LoginAsync(request);
        return Ok(new LoginResponse { User = user });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MessageResponse>> Register(RegisterRequest request)
    {
        await _auth.RegisterAsync(request);
        return StatusCode(201, new MessageResponse("Registration successful. Please login."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<MessageResponse>> Logout()
    {
        await _auth.LogoutAsync();
        return Ok(new MessageResponse("Logged out successfully."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me() => Ok(await _auth.GetCurrentUserAsync());

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<MessageResponse>> ChangePassword(ChangePasswordRequest request)
    {
        await _auth.ChangePasswordAsync(request);
        return Ok(new MessageResponse("Password changed successfully. Please login again."));
    }
}
