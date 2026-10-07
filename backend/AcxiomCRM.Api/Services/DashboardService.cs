using System.Globalization;
using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(DashboardQuery query);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(ApplicationDbContext db, IScopeService scope, ICurrentUserService currentUser)
    {
        _db = db;
        _scope = scope;
        _currentUser = currentUser;
    }

    // Small class to hold the opportunity columns we need for the calculations
    private class OpportunityRow
    {
        public int AssignedTo { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Probability { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
    }

    public async Task<DashboardDto> GetDashboardAsync(DashboardQuery query)
    {
        var user = await _currentUser.GetAsync();
        var (fromDate, toDate) = GetDateRange(query);
        var fromUtc = QueryFilters.StartUtc(fromDate);
        var toUtc = QueryFilters.EndUtcExclusive(toDate);
        var today = DateHelper.Today();

        // ---------- record counts (scoped + created in the selected range) ----------
        var customers = await _scope.ApplyScopeAsync(_db.Customers.AsNoTracking());
        var leads = await _scope.ApplyScopeAsync(_db.Leads.AsNoTracking());
        var followUps = await _scope.ApplyScopeAsync(_db.FollowUps.AsNoTracking());
        if (fromUtc != null)
        {
            customers = customers.Where(c => c.CreatedDate >= fromUtc && c.CreatedDate < toUtc);
            leads = leads.Where(l => l.CreatedDate >= fromUtc && l.CreatedDate < toUtc);
        }

        // Opportunities are loaded once (only the columns we need) and summed in memory - the data set is small
        var allOpportunities = await (await _scope.ApplyScopeAsync(_db.Opportunities.AsNoTracking()))
            .Select(o => new OpportunityRow
            {
                AssignedTo = o.AssignedTo,
                Stage = o.Stage,
                Status = o.Status,
                Amount = o.Amount,
                Probability = o.Probability,
                CreatedDate = o.CreatedDate,
                ClosedDate = o.ClosedDate,
            })
            .ToListAsync();
        var opportunities = fromUtc == null
            ? allOpportunities
            : allOpportunities.Where(o => o.CreatedDate >= fromUtc && o.CreatedDate < toUtc).ToList();
        var openOpportunities = opportunities.Where(o => o.Status == "Open").ToList();

        var kpis = new DashboardKpisDto
        {
            TotalCustomers = await customers.CountAsync(),
            TotalLeads = await leads.CountAsync(),
            OpenLeads = await leads.CountAsync(l =>
                l.Status != LeadStatuses.Converted && l.Status != LeadStatuses.Lost && l.Status != LeadStatuses.Unqualified),
            TotalOpportunities = opportunities.Count,
            OpenOpportunities = openOpportunities.Count,
            WonOpportunities = opportunities.Count(o => o.Status == "Won"),
            LostOpportunities = opportunities.Count(o => o.Status == "Lost"),
            TotalPipelineValue = Round(openOpportunities.Sum(o => o.Amount)),
            WeightedPipelineValue = Round(openOpportunities.Sum(o => o.Amount * o.Probability / 100m)),
            WonAmount = Round(opportunities.Where(o => o.Status == "Won").Sum(o => o.Amount)),
            // follow-up counts always show the current state, the date range is not applied
            PendingFollowUps = await followUps.CountAsync(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate >= today),
            OverdueFollowUps = await followUps.CountAsync(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate < today),
        };

        var charts = new DashboardChartsDto
        {
            LeadStatus = await GetLeadStatusChartAsync(leads),
            OpportunityPipeline = GetPipelineChart(opportunities),
            // last 6 months by closed date (range filter not applied)
            MonthlySales = GetMonthlySalesChart(allOpportunities),
        };

        return new DashboardDto
        {
            Range = new DashboardRangeDto
            {
                From = fromDate?.ToString("yyyy-MM-dd"),
                To = toDate?.ToString("yyyy-MM-dd"),
            },
            Kpis = kpis,
            Charts = charts,
            UpcomingFollowUps = await GetUpcomingFollowUpsAsync(today),
            RecentActivities = await GetRecentActivitiesAsync(),
            AdminStats = user.IsAdmin ? await GetAdminStatsAsync() : null,
            TeamPerformance = user.Role == Roles.SalesExecutive ? null : await GetTeamPerformanceAsync(opportunities),
        };
    }

    // Converts ?range=today|week|month|custom|all into dates (India time). (null, null) = all time.
    private static (DateOnly? From, DateOnly? To) GetDateRange(DashboardQuery query)
    {
        var today = DateHelper.Today();
        switch (query.Range)
        {
            case "today":
                return (today, today);
            case "week":
                // week starts on Monday
                var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
                return (today.AddDays(-daysSinceMonday), today);
            case "month":
                return (new DateOnly(today.Year, today.Month, 1), today);
            case "custom":
                var from = QueryFilters.ReadDate(query.From, "from");
                var to = QueryFilters.ReadDate(query.To, "to");
                if (from == null || to == null)
                {
                    throw AppException.BadRequest("Please select both From and To dates for a custom range.");
                }
                if (from > to) throw AppException.BadRequest("From date cannot be after To date.");
                return (from, to);
            default:
                return (null, null);
        }
    }

    private static async Task<LeadStatusChartDto> GetLeadStatusChartAsync(IQueryable<Models.Lead> leads)
    {
        var counts = await leads
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // same order as the dashboard chart: Converted shown last
        var labels = LeadStatuses.All.Where(s => s != LeadStatuses.Converted).Append(LeadStatuses.Converted).ToList();
        return new LeadStatusChartDto
        {
            Labels = labels,
            Data = labels.Select(s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0).ToList(),
        };
    }

    private static PipelineChartDto GetPipelineChart(List<OpportunityRow> opportunities)
    {
        return new PipelineChartDto
        {
            Labels = OpportunityStages.All.ToList(),
            Counts = OpportunityStages.All.Select(s => opportunities.Count(o => o.Stage == s)).ToList(),
            Amounts = OpportunityStages.All.Select(s => Round(opportunities.Where(o => o.Stage == s).Sum(o => o.Amount))).ToList(),
        };
    }

    private static MonthlySalesChartDto GetMonthlySalesChart(List<OpportunityRow> opportunities)
    {
        var today = DateHelper.Today();
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
        var months = Enumerable.Range(0, 6).Select(i => firstMonth.AddMonths(i)).ToList();

        var chart = new MonthlySalesChartDto
        {
            Labels = months.Select(m => m.ToString("MMM yyyy", CultureInfo.InvariantCulture)).ToList(),
            Won = months.Select(_ => 0m).ToList(),
            Lost = months.Select(_ => 0m).ToList(),
        };

        foreach (var opp in opportunities.Where(o => o.ClosedDate != null && (o.Status == "Won" || o.Status == "Lost")))
        {
            var closed = DateHelper.ToLocalDate(opp.ClosedDate!.Value);
            var index = months.FindIndex(m => m.Year == closed.Year && m.Month == closed.Month);
            if (index == -1) continue;

            if (opp.Status == "Won") chart.Won[index] += opp.Amount;
            else chart.Lost[index] += opp.Amount;
        }

        chart.Won = chart.Won.Select(Round).ToList();
        chart.Lost = chart.Lost.Select(Round).ToList();
        return chart;
    }

    private async Task<List<FollowUpDto>> GetUpcomingFollowUpsAsync(DateOnly today)
    {
        var query = _db.FollowUps.AsNoTracking()
            .Include(f => f.Customer).Include(f => f.Lead).Include(f => f.Opportunity).Include(f => f.AssignedUser);
        var followUps = await (await _scope.ApplyScopeAsync(query))
            .Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate >= today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();
        return followUps.Select(FollowUpDto.From).ToList();
    }

    private async Task<List<ActivityDto>> GetRecentActivitiesAsync()
    {
        var now = DateTime.UtcNow;
        var query = _db.Activities.AsNoTracking()
            .Include(a => a.Customer).Include(a => a.Lead).Include(a => a.AssignedUser);
        var activities = await (await _scope.ApplyScopeAsync(query))
            .Where(a => a.ActivityDate <= now)
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();
        return activities.Select(ActivityDto.From).ToList();
    }

    // Security / user statistics shown only to Admin
    private async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var users = await _db.Users.AsNoTracking().Select(u => new { u.IsActive, u.LockoutEnd }).ToListAsync();
        var startOfToday = DateHelper.LocalDateToUtc(DateHelper.Today());

        return new AdminStatsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            // LockoutEnd is a DateTimeOffset which SQLite can't compare, so it is checked in memory
            LockedUsers = users.Count(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow),
            FailedLoginsToday = await _db.AuditLogs.CountAsync(a => a.Action == AuditActions.LoginFailed && a.CreatedDate >= startOfToday),
        };
    }

    // Open pipeline and won amount per sales person (Admin: everyone, Manager: his team)
    private async Task<List<TeamPerformanceDto>> GetTeamPerformanceAsync(List<OpportunityRow> opportunities)
    {
        var ids = await _scope.GetScopeUserIdsAsync();
        var usersQuery = _db.Users.AsNoTracking().Where(u => u.IsActive);
        if (ids != null) usersQuery = usersQuery.Where(u => ids.Contains(u.Id));

        var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
        var userDtos = await UserMapper.ToDtosAsync(_db, users);
        var members = userDtos.Where(u => u.Role != Roles.Admin).ToList();

        return members
            .Select(m =>
            {
                var mine = opportunities.Where(o => o.AssignedTo == m.Id).ToList();
                var open = mine.Where(o => o.Status == "Open").ToList();
                return new TeamPerformanceDto
                {
                    UserId = m.Id,
                    UserName = m.Name,
                    OpenOpportunities = open.Count,
                    PipelineValue = Round(open.Sum(o => o.Amount)),
                    WonAmount = Round(mine.Where(o => o.Status == "Won").Sum(o => o.Amount)),
                };
            })
            .OrderByDescending(t => t.PipelineValue)
            .ToList();
    }

    private static decimal Round(decimal value) => Math.Round(value, 2);
}
