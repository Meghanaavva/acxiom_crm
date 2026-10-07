using System.Security.Claims;
using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public class CurrentUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? ManagerId { get; set; }

    public bool IsAdmin => Role == Roles.Admin;
    public bool IsManager => Role == Roles.Manager;
}

public interface ICurrentUserService
{
    // Logged in user (cached for the request). Throws 401 when not logged in.
    Task<CurrentUser> GetAsync();
    int? UserId { get; }
    string? IpAddress { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    private readonly ApplicationDbContext _db;
    private CurrentUser? _cached;

    public CurrentUserService(IHttpContextAccessor http, ApplicationDbContext db)
    {
        _http = http;
        _db = db;
    }

    public int? UserId
    {
        get
        {
            var id = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var value) ? value : null;
        }
    }

    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4().ToString();

    public async Task<CurrentUser> GetAsync()
    {
        if (_cached != null) return _cached;
        var id = UserId ?? throw AppException.Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.IsActive)
            ?? throw AppException.Unauthorized("Your session is no longer valid. Please login again.");

        var role = await (from ur in _db.UserRoles
                          join r in _db.Roles on ur.RoleId equals r.Id
                          where ur.UserId == id
                          select r.Name).FirstOrDefaultAsync();

        _cached = new CurrentUser
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role ?? Roles.SalesExecutive,
            ManagerId = user.ManagerId,
        };
        return _cached;
    }
}
