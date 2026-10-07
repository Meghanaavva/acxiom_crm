using AcxiomCRM.Api.Dtos;

namespace AcxiomCRM.Api.ViewModels;

public class DashboardViewModel
{
    public DashboardDto Data { get; set; } = new();
    public string Range { get; set; } = "all";
    public string? From { get; set; }
    public string? To { get; set; }
    public string? ErrorMessage { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
