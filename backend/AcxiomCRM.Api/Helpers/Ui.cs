using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace AcxiomCRM.Api.Helpers;

// Small display helpers used by the Razor views
public static class Ui
{
    private static readonly CultureInfo India = new("en-IN");

    private static readonly Dictionary<string, string> BadgeColors = new()
    {
        ["Active"] = "success", ["Inactive"] = "secondary", ["Prospect"] = "info",
        ["New"] = "primary", ["Contacted"] = "info", ["Qualified"] = "success", ["Unqualified"] = "secondary",
        ["Converted"] = "dark", ["Lost"] = "danger",
        ["Qualification"] = "primary", ["Proposal"] = "info", ["Negotiation"] = "warning", ["Won"] = "success", ["Open"] = "primary",
        ["Planned"] = "primary", ["Completed"] = "success", ["Missed"] = "danger", ["Cancelled"] = "secondary", ["Overdue"] = "danger",
        ["High"] = "danger", ["Medium"] = "warning", ["Low"] = "secondary",
        ["Success"] = "success", ["Failure"] = "danger",
        ["Admin"] = "danger", ["Manager"] = "primary", ["SalesExecutive"] = "info",
    };

    public static IHtmlContent Badge(string? status)
    {
        if (string.IsNullOrEmpty(status)) return new HtmlString("-");
        var color = BadgeColors.GetValueOrDefault(status, "secondary");
        var text = color is "warning" or "info" ? " text-dark" : "";
        var label = System.Net.WebUtility.HtmlEncode(RoleLabel(status));
        return new HtmlString($"<span class=\"badge bg-{color}{text}\">{label}</span>");
    }

    public static string RoleLabel(string? role) => role == Roles.SalesExecutive ? "Sales Executive" : role ?? "";

    public static string Currency(decimal value) => value.ToString("C0", India);

    public static string Date(DateOnly? value) => value?.ToString("dd MMM yyyy", India) ?? "-";

    public static string Date(DateTime? utc) => utc.HasValue ? DateHelper.ToLocal(utc.Value).ToString("dd MMM yyyy", India) : "-";

    public static string DateTime(DateTime? utc) =>
        utc.HasValue ? DateHelper.ToLocal(utc.Value).ToString("dd MMM yyyy, hh:mm tt", India) : "-";

    // Current URL with some query values replaced (keeps the other filters)
    public static string WithQuery(HttpRequest request, params (string Key, string? Value)[] values)
    {
        var query = QueryHelpers.ParseQuery(request.QueryString.Value)
            .ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            if (string.IsNullOrEmpty(value)) query.Remove(key);
            else query[key] = value;
        }
        return request.Path + QueryString.Create(query!).ToUriComponent();
    }

    // Clickable column header that toggles asc / desc sorting
    public static IHtmlContent SortHeader(HttpRequest request, string field, string label)
    {
        var currentField = request.Query["sortBy"].ToString();
        var currentOrder = request.Query["sortOrder"].ToString();
        var active = currentField == field;
        var nextOrder = active && currentOrder == "asc" ? "desc" : "asc";
        var icon = active ? (currentOrder == "asc" ? "bi-caret-up-fill" : "bi-caret-down-fill") : "bi-chevron-expand text-muted";
        var url = System.Net.WebUtility.HtmlEncode(WithQuery(request, ("sortBy", field), ("sortOrder", nextOrder), ("page", null)));
        return new HtmlString($"<th class=\"text-nowrap\"><a href=\"{url}\" class=\"text-reset text-decoration-none\">{label} <i class=\"bi {icon}\"></i></a></th>");
    }
}
