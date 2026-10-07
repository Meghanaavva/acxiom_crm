using System.Text.Json;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Api.ViewModels;

public class UserIndexViewModel
{
    public PagedResult<UserDto> Result { get; set; } = new();
    public UserListQuery Query { get; set; } = new();
    public bool IsAdmin { get; set; }
}

// Add / Edit user form. Inherits CreateUserRequest so the same DataAnnotations are used.
// On edit the password field is not shown and its validation is removed in the controller.
public class UserFormViewModel : CreateUserRequest
{
    public int? Id { get; set; }

    // checkbox needs a plain bool (IsActive in the request is nullable)
    public bool Active { get; set; } = true;

    public List<SelectListItem> RoleOptions { get; set; } = new();
    public List<SelectListItem> Managers { get; set; } = new();
}

public class RolesViewModel
{
    public List<RoleDto> Roles { get; set; } = new();
    public PermissionMatrixDto Permissions { get; set; } = new();
}

public class AuditLogIndexViewModel
{
    public PagedResult<AuditLogDto> Result { get; set; } = new();
    public AuditLogQuery Query { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
    public AuditFiltersDto Filters { get; set; } = new();

    private static readonly JsonSerializerOptions PrettyOptions = new() { WriteIndented = true };

    // Formats the stored JSON text so it is easy to read in the details dialog
    public static string Pretty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "-";
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, PrettyOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
