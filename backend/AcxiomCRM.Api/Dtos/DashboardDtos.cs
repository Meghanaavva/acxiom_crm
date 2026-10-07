namespace AcxiomCRM.Api.Dtos;

public class DashboardQuery
{
    public string? Range { get; set; } // all / today / week / month / custom
    public string? From { get; set; }
    public string? To { get; set; }
}

public class DashboardDto
{
    public DashboardRangeDto Range { get; set; } = new();
    public DashboardKpisDto Kpis { get; set; } = new();
    public DashboardChartsDto Charts { get; set; } = new();
    public List<FollowUpDto> UpcomingFollowUps { get; set; } = new();
    public List<ActivityDto> RecentActivities { get; set; } = new();
    public AdminStatsDto? AdminStats { get; set; }
    public List<TeamPerformanceDto>? TeamPerformance { get; set; }
}

public class DashboardRangeDto
{
    public string? From { get; set; }
    public string? To { get; set; }
}

public class DashboardKpisDto
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public decimal WonAmount { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
}

public class DashboardChartsDto
{
    public LeadStatusChartDto LeadStatus { get; set; } = new();
    public PipelineChartDto OpportunityPipeline { get; set; } = new();
    public MonthlySalesChartDto MonthlySales { get; set; } = new();
}

public class LeadStatusChartDto
{
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
}

public class PipelineChartDto
{
    public List<string> Labels { get; set; } = new();
    public List<int> Counts { get; set; } = new();
    public List<decimal> Amounts { get; set; } = new();
}

public class MonthlySalesChartDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Won { get; set; } = new();
    public List<decimal> Lost { get; set; } = new();
}

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int FailedLoginsToday { get; set; }
}

public class TeamPerformanceDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int OpenOpportunities { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal WonAmount { get; set; }
}
