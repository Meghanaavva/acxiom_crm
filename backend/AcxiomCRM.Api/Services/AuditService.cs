using System.Text.Json;
using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Models;

namespace AcxiomCRM.Api.Services;

public class AuditEntry
{
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public object? RecordId { get; set; }
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public string Result { get; set; } = "Success";
    public string? Details { get; set; }

    // Only needed when nobody is logged in yet (e.g. failed login)
    public int? UserId { get; set; }
    public string? UserName { get; set; }
}

public interface IAuditService
{
    Task LogAsync(AuditEntry entry);
}

public class AuditService : IAuditService
{
    private static readonly string[] SensitiveFields =
        { "password", "passwordHash", "confirmPassword", "currentPassword", "newPassword", "securityStamp", "token" };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext db, ICurrentUserService currentUser, ILogger<AuditService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task LogAsync(AuditEntry entry)
    {
        try
        {
            int? userId = entry.UserId;
            string? userName = entry.UserName;
            if (userId == null && _currentUser.UserId != null)
            {
                var user = await _currentUser.GetAsync();
                userId = user.Id;
                userName = user.Name;
            }

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = entry.Action,
                EntityName = entry.EntityName,
                RecordId = entry.RecordId?.ToString(),
                OldValue = ToSafeJson(entry.OldValue),
                NewValue = ToSafeJson(entry.NewValue),
                Result = entry.Result,
                Details = entry.Details is { Length: > 500 } ? entry.Details[..500] : entry.Details,
                IpAddress = _currentUser.IpAddress,
                CreatedDate = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit failure should not break the user's request
            _logger.LogError(ex, "Failed to write audit log");
        }
    }

    // Serializes the value and removes passwords / tokens before saving
    private static string? ToSafeJson(object? value)
    {
        if (value == null) return null;
        var element = JsonSerializer.SerializeToElement(value, JsonOptions);
        if (element.ValueKind != JsonValueKind.Object) return element.ToString();

        var dict = new Dictionary<string, JsonElement>();
        foreach (var prop in element.EnumerateObject())
        {
            if (!SensitiveFields.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)) dict[prop.Name] = prop.Value;
        }
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    // Returns only the changed fields: (old, new). Used to keep audit records small.
    public static (Dictionary<string, object?> OldValues, Dictionary<string, object?> NewValues) Diff(
        Dictionary<string, object?> before, Dictionary<string, object?> after)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        foreach (var (key, value) in after)
        {
            before.TryGetValue(key, out var oldValue);
            if (!Equals(Normalize(oldValue), Normalize(value)))
            {
                oldValues[key] = oldValue;
                newValues[key] = value;
            }
        }
        return (oldValues, newValues);
    }

    private static string Normalize(object? value) => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("0.##"),
        _ => value.ToString() ?? string.Empty,
    };
}
