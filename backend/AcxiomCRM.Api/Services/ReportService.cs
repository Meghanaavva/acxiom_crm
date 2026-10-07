using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface IReportService
{
    Task<ReportOutput> CustomerReportAsync(ReportQuery query);
    Task<ReportOutput> LeadReportAsync(ReportQuery query);
    Task<ReportOutput> FollowUpReportAsync(ReportQuery query);
    Task<ReportOutput> OpportunityReportAsync(ReportQuery query);
    Task<ReportOutput> PipelineReportAsync(ReportQuery query);
    Task<ReportOutput> ConversionReportAsync(ReportQuery query);
    Task<ReportOutput> UserActivityReportAsync(ReportQuery query);
    Task<ReportOutput> AuditReportAsync(AuditReportQuery query);
}

/// <summary>
/// All reports are scoped by role (own / team / all).
/// Table reports return paged JSON, or every matching row as CSV when ?format=csv.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly IAuditLogService _auditLogs;

    public ReportService(ApplicationDbContext db, IScopeService scope, IAuditLogService auditLogs)
    {
        _db = db;
        _scope = scope;
        _auditLogs = auditLogs;
    }

    // ---------------- Customer report ----------------
    public async Task<ReportOutput> CustomerReportAsync(ReportQuery query)
    {
        var customers = await ApplyCommonFiltersAsync(_db.Customers.AsNoTracking().Include(c => c.AssignedUser), query);
        customers = FilterCreatedDate(customers, query, c => c.CreatedDate);
        if (!string.IsNullOrEmpty(query.Status)) customers = customers.Where(c => c.Status == query.Status);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<Customer, object?>>>
        {
            ["customerCode"] = c => c.CustomerCode,
            ["customerName"] = c => c.CustomerName,
            ["companyName"] = c => c.CompanyName,
            ["status"] = c => c.Status,
            ["createdAt"] = c => c.CreatedDate,
        };
        customers = customers.SortBy(query, sortColumns, "createdAt");

        CustomerReportRow Map(Customer c) => new()
        {
            Id = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            CompanyName = c.CompanyName,
            Email = c.Email,
            Phone = c.Phone,
            Status = c.Status,
            AssignedToName = c.AssignedUser?.FullName,
            CreatedAt = c.CreatedDate,
        };

        if (!query.IsCsv) return Json(await customers.ToPagedResultAsync(query, Map));

        var rows = (await customers.ToListAsync()).Select(Map);
        return Csv("customer-report.csv", CsvHelper.Build(rows,
            new CsvHelper.Column<CustomerReportRow>("Code", r => r.CustomerCode),
            new CsvHelper.Column<CustomerReportRow>("Customer Name", r => r.CustomerName),
            new CsvHelper.Column<CustomerReportRow>("Company", r => r.CompanyName),
            new CsvHelper.Column<CustomerReportRow>("Email", r => r.Email),
            new CsvHelper.Column<CustomerReportRow>("Phone", r => r.Phone),
            new CsvHelper.Column<CustomerReportRow>("Status", r => r.Status),
            new CsvHelper.Column<CustomerReportRow>("Owner", r => r.AssignedToName),
            new CsvHelper.Column<CustomerReportRow>("Created Date", r => r.CreatedAt)));
    }

    // ---------------- Lead report ----------------
    public async Task<ReportOutput> LeadReportAsync(ReportQuery query)
    {
        var leads = await ApplyCommonFiltersAsync(_db.Leads.AsNoTracking().Include(l => l.AssignedUser), query);
        leads = FilterCreatedDate(leads, query, l => l.CreatedDate);
        if (!string.IsNullOrEmpty(query.Status)) leads = leads.Where(l => l.Status == query.Status);
        if (!string.IsNullOrEmpty(query.Source)) leads = leads.Where(l => l.Source == query.Source);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<Lead, object?>>>
        {
            ["leadCode"] = l => l.LeadCode,
            ["leadName"] = l => l.LeadName,
            ["companyName"] = l => l.CompanyName,
            ["source"] = l => l.Source,
            ["status"] = l => l.Status,
            ["priority"] = l => l.Priority,
            ["expectedValue"] = l => l.ExpectedValue,
            ["createdAt"] = l => l.CreatedDate,
        };
        leads = leads.SortBy(query, sortColumns, "createdAt");

        LeadReportRow Map(Lead l) => new()
        {
            Id = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            AssignedToName = l.AssignedUser?.FullName,
            Converted = l.Status == LeadStatuses.Converted,
            ConvertedAt = l.ConvertedDate,
            CreatedAt = l.CreatedDate,
        };

        if (!query.IsCsv) return Json(await leads.ToPagedResultAsync(query, Map));

        var rows = (await leads.ToListAsync()).Select(Map);
        return Csv("lead-report.csv", CsvHelper.Build(rows,
            new CsvHelper.Column<LeadReportRow>("Code", r => r.LeadCode),
            new CsvHelper.Column<LeadReportRow>("Lead Name", r => r.LeadName),
            new CsvHelper.Column<LeadReportRow>("Company", r => r.CompanyName),
            new CsvHelper.Column<LeadReportRow>("Source", r => r.Source),
            new CsvHelper.Column<LeadReportRow>("Status", r => r.Status),
            new CsvHelper.Column<LeadReportRow>("Priority", r => r.Priority),
            new CsvHelper.Column<LeadReportRow>("Expected Value", r => r.ExpectedValue),
            new CsvHelper.Column<LeadReportRow>("Owner", r => r.AssignedToName),
            new CsvHelper.Column<LeadReportRow>("Converted", r => r.Converted),
            new CsvHelper.Column<LeadReportRow>("Converted Date", r => r.ConvertedAt),
            new CsvHelper.Column<LeadReportRow>("Created Date", r => r.CreatedAt)));
    }

    // ---------------- Follow-up report (dates filter on follow-up date) ----------------
    public async Task<ReportOutput> FollowUpReportAsync(ReportQuery query)
    {
        var baseQuery = _db.FollowUps.AsNoTracking()
            .Include(f => f.Customer).Include(f => f.Lead).Include(f => f.Opportunity).Include(f => f.AssignedUser);
        var followUps = await ApplyCommonFiltersAsync(baseQuery, query);

        var (from, to) = QueryFilters.ReadRange(query.DateFrom, query.DateTo);
        if (from != null) followUps = followUps.Where(f => f.FollowUpDate >= from);
        if (to != null) followUps = followUps.Where(f => f.FollowUpDate <= to);

        var today = DateHelper.Today();
        if (query.Status == "Overdue")
        {
            followUps = followUps.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate < today);
        }
        else if (!string.IsNullOrEmpty(query.Status))
        {
            followUps = followUps.Where(f => f.Status == query.Status);
        }

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<FollowUp, object?>>>
        {
            ["followUpDate"] = f => f.FollowUpDate,
            ["subject"] = f => f.Subject,
            ["followUpType"] = f => f.FollowUpType,
            ["status"] = f => f.Status,
        };
        followUps = followUps.SortBy(query, sortColumns, "followUpDate", defaultAscending: true);

        FollowUpReportRow Map(FollowUp f)
        {
            var dto = FollowUpDto.From(f);
            var related = new List<string>();
            if (dto.CustomerName != null) related.Add($"Customer: {dto.CustomerName}");
            if (dto.LeadName != null) related.Add($"Lead: {dto.LeadName}");
            if (dto.OpportunityName != null) related.Add($"Opportunity: {dto.OpportunityName}");
            return new FollowUpReportRow
            {
                Id = dto.Id,
                Subject = dto.Subject,
                FollowUpType = dto.FollowUpType,
                FollowUpDate = dto.FollowUpDate,
                Status = dto.Status,
                IsOverdue = dto.IsOverdue,
                RelatedTo = related.Count > 0 ? string.Join(", ", related) : "-",
                AssignedToName = dto.AssignedToName,
            };
        }

        if (!query.IsCsv) return Json(await followUps.ToPagedResultAsync(query, Map));

        var rows = (await followUps.ToListAsync()).Select(Map);
        return Csv("followup-report.csv", CsvHelper.Build(rows,
            new CsvHelper.Column<FollowUpReportRow>("Subject", r => r.Subject),
            new CsvHelper.Column<FollowUpReportRow>("Type", r => r.FollowUpType),
            new CsvHelper.Column<FollowUpReportRow>("Follow-up Date", r => r.FollowUpDate),
            new CsvHelper.Column<FollowUpReportRow>("Status", r => r.Status),
            new CsvHelper.Column<FollowUpReportRow>("Overdue", r => r.IsOverdue),
            new CsvHelper.Column<FollowUpReportRow>("Related To", r => r.RelatedTo),
            new CsvHelper.Column<FollowUpReportRow>("Assigned To", r => r.AssignedToName)));
    }

    // ---------------- Opportunity report ----------------
    public async Task<ReportOutput> OpportunityReportAsync(ReportQuery query)
    {
        var baseQuery = _db.Opportunities.AsNoTracking().Include(o => o.Customer).Include(o => o.AssignedUser);
        var opportunities = await ApplyCommonFiltersAsync(baseQuery, query);
        opportunities = FilterCreatedDate(opportunities, query, o => o.CreatedDate);
        if (!string.IsNullOrEmpty(query.Stage)) opportunities = opportunities.Where(o => o.Stage == query.Stage);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<Opportunity, object?>>>
        {
            ["opportunityName"] = o => o.OpportunityName,
            ["stage"] = o => o.Stage,
            ["amount"] = o => o.Amount,
            ["probability"] = o => o.Probability,
            ["expectedCloseDate"] = o => o.ExpectedCloseDate,
            ["createdAt"] = o => o.CreatedDate,
        };
        opportunities = opportunities.SortBy(query, sortColumns, "createdAt");

        OpportunityReportRow Map(Opportunity o) => new()
        {
            Id = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerName = o.Customer?.CustomerName,
            Stage = o.Stage,
            Amount = o.Amount,
            Probability = o.Probability,
            WeightedAmount = OpportunityDto.Weighted(o.Amount, o.Probability),
            ExpectedCloseDate = o.ExpectedCloseDate,
            AssignedToName = o.AssignedUser?.FullName,
            CreatedAt = o.CreatedDate,
        };

        if (!query.IsCsv) return Json(await opportunities.ToPagedResultAsync(query, Map));

        var rows = (await opportunities.ToListAsync()).Select(Map);
        return Csv("opportunity-report.csv", CsvHelper.Build(rows,
            new CsvHelper.Column<OpportunityReportRow>("Opportunity", r => r.OpportunityName),
            new CsvHelper.Column<OpportunityReportRow>("Customer", r => r.CustomerName),
            new CsvHelper.Column<OpportunityReportRow>("Stage", r => r.Stage),
            new CsvHelper.Column<OpportunityReportRow>("Amount", r => r.Amount),
            new CsvHelper.Column<OpportunityReportRow>("Probability (%)", r => r.Probability),
            new CsvHelper.Column<OpportunityReportRow>("Weighted Amount", r => r.WeightedAmount),
            new CsvHelper.Column<OpportunityReportRow>("Expected Close Date", r => r.ExpectedCloseDate),
            new CsvHelper.Column<OpportunityReportRow>("Owner", r => r.AssignedToName),
            new CsvHelper.Column<OpportunityReportRow>("Created Date", r => r.CreatedAt)));
    }

    // ---------------- Pipeline report (open opportunities, stage wise and owner wise) ----------------
    public async Task<ReportOutput> PipelineReportAsync(ReportQuery query)
    {
        var baseQuery = _db.Opportunities.AsNoTracking().Include(o => o.AssignedUser).Where(o => o.Status == "Open");
        var opportunities = await ApplyCommonFiltersAsync(baseQuery, query);
        opportunities = FilterCreatedDate(opportunities, query, o => o.CreatedDate);
        var list = await opportunities.ToListAsync();

        var report = new PipelineReportDto();

        foreach (var stage in OpportunityStages.Open)
        {
            var inStage = list.Where(o => o.Stage == stage).ToList();
            report.ByStage.Add(new PipelineStageRow
            {
                Stage = stage,
                Count = inStage.Count,
                Amount = Round(inStage.Sum(o => o.Amount)),
                WeightedAmount = Round(inStage.Sum(o => o.Amount * o.Probability / 100m)),
            });
        }

        report.ByOwner = list
            .GroupBy(o => o.AssignedTo)
            .Select(g => new PipelineOwnerRow
            {
                OwnerId = g.Key,
                OwnerName = g.First().AssignedUser?.FullName ?? "Unknown",
                Count = g.Count(),
                Amount = Round(g.Sum(o => o.Amount)),
                WeightedAmount = Round(g.Sum(o => o.Amount * o.Probability / 100m)),
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        report.Totals = new PipelineTotals
        {
            Count = list.Count,
            Amount = Round(list.Sum(o => o.Amount)),
            WeightedAmount = Round(list.Sum(o => o.Amount * o.Probability / 100m)),
        };

        if (!query.IsCsv) return Json(report);

        // CSV export = owner wise table
        return Csv("pipeline-report.csv", CsvHelper.Build(report.ByOwner,
            new CsvHelper.Column<PipelineOwnerRow>("Owner", r => r.OwnerName),
            new CsvHelper.Column<PipelineOwnerRow>("Open Opportunities", r => r.Count),
            new CsvHelper.Column<PipelineOwnerRow>("Pipeline Amount", r => r.Amount),
            new CsvHelper.Column<PipelineOwnerRow>("Weighted Amount", r => r.WeightedAmount)));
    }

    // ---------------- Lead conversion / sales outcome report ----------------
    public async Task<ReportOutput> ConversionReportAsync(ReportQuery query)
    {
        var leadQuery = await ApplyCommonFiltersAsync(_db.Leads.AsNoTracking(), query);
        leadQuery = FilterCreatedDate(leadQuery, query, l => l.CreatedDate);
        var leads = await leadQuery.Select(l => new { l.Source, l.Status }).ToListAsync();

        var oppQuery = await ApplyCommonFiltersAsync(_db.Opportunities.AsNoTracking(), query);
        oppQuery = FilterCreatedDate(oppQuery, query, o => o.CreatedDate);
        var opportunities = await oppQuery.Select(o => new { o.Status, o.Amount }).ToListAsync();

        var convertedLeads = leads.Count(l => l.Status == LeadStatuses.Converted);
        var wonOpportunities = opportunities.Count(o => o.Status == "Won");
        var lostOpportunities = opportunities.Count(o => o.Status == "Lost");

        var report = new ConversionReportDto
        {
            TotalLeads = leads.Count,
            ConvertedLeads = convertedLeads,
            LostLeads = leads.Count(l => l.Status == LeadStatuses.Lost),
            ConversionRate = Percent(convertedLeads, leads.Count),
            TotalOpportunities = opportunities.Count,
            WonOpportunities = wonOpportunities,
            LostOpportunities = lostOpportunities,
            // win rate is calculated on closed opportunities only
            WinRate = Percent(wonOpportunities, wonOpportunities + lostOpportunities),
            WonAmount = Round(opportunities.Where(o => o.Status == "Won").Sum(o => o.Amount)),
            BySource = LeadSources.All.Select(source =>
            {
                var total = leads.Count(l => l.Source == source);
                var converted = leads.Count(l => l.Source == source && l.Status == LeadStatuses.Converted);
                return new SourceConversionRow
                {
                    Source = source,
                    Total = total,
                    Converted = converted,
                    ConversionRate = Percent(converted, total),
                };
            }).ToList(),
        };

        if (!query.IsCsv) return Json(report);

        // CSV export = source wise table
        return Csv("conversion-report.csv", CsvHelper.Build(report.BySource,
            new CsvHelper.Column<SourceConversionRow>("Lead Source", r => r.Source),
            new CsvHelper.Column<SourceConversionRow>("Total Leads", r => r.Total),
            new CsvHelper.Column<SourceConversionRow>("Converted", r => r.Converted),
            new CsvHelper.Column<SourceConversionRow>("Conversion Rate (%)", r => r.ConversionRate)));
    }

    // ---------------- User activity report (Admin: everyone, Manager: his team) ----------------
    public async Task<ReportOutput> UserActivityReportAsync(ReportQuery query)
    {
        var ids = await _scope.GetScopeUserIdsAsync();
        var usersQuery = _db.Users.AsNoTracking();
        if (ids != null) usersQuery = usersQuery.Where(u => ids.Contains(u.Id));
        var users = await UserMapper.ToDtosAsync(_db, await usersQuery.ToListAsync());
        var userIds = users.Select(u => u.Id).ToList();

        var logsQuery = _db.AuditLogs.AsNoTracking().Where(a => a.UserId != null && userIds.Contains(a.UserId.Value));
        logsQuery = FilterCreatedDate(logsQuery, query, a => a.CreatedDate);
        // one small row per log entry, counted in memory below
        var logs = await logsQuery.Select(a => new { a.UserId, a.Action, a.CreatedDate }).ToListAsync();

        var rows = users.Select(u =>
        {
            var mine = logs.Where(l => l.UserId == u.Id).ToList();
            return new UserActivityRow
            {
                UserId = u.Id,
                UserName = u.Name,
                Role = string.IsNullOrEmpty(u.Role) ? null : u.Role,
                TotalActions = mine.Count,
                Logins = mine.Count(l => l.Action == AuditActions.Login),
                FailedLogins = mine.Count(l => l.Action == AuditActions.LoginFailed),
                Creates = mine.Count(l => l.Action == AuditActions.Create),
                Updates = mine.Count(l => l.Action == AuditActions.Update),
                Deletes = mine.Count(l => l.Action == AuditActions.Delete),
                LastActivity = mine.Count > 0 ? mine.Max(l => l.CreatedDate) : null,
            };
        });

        var sorted = SortUserActivity(rows, query).ToList();

        if (query.IsCsv)
        {
            return Csv("user-activity-report.csv", CsvHelper.Build(sorted,
                new CsvHelper.Column<UserActivityRow>("User", r => r.UserName),
                new CsvHelper.Column<UserActivityRow>("Role", r => r.Role),
                new CsvHelper.Column<UserActivityRow>("Total Actions", r => r.TotalActions),
                new CsvHelper.Column<UserActivityRow>("Logins", r => r.Logins),
                new CsvHelper.Column<UserActivityRow>("Failed Logins", r => r.FailedLogins),
                new CsvHelper.Column<UserActivityRow>("Creates", r => r.Creates),
                new CsvHelper.Column<UserActivityRow>("Updates", r => r.Updates),
                new CsvHelper.Column<UserActivityRow>("Deletes", r => r.Deletes),
                new CsvHelper.Column<UserActivityRow>("Last Activity", r => r.LastActivity)));
        }

        // one row per user, so paging is done in memory
        return Json(new PagedResult<UserActivityRow>
        {
            Data = sorted.Skip(query.Skip).Take(query.PageSize).ToList(),
            Pagination = new PaginationInfo
            {
                Page = query.Page,
                PageSize = query.PageSize,
                Total = sorted.Count,
                TotalPages = Math.Max(1, (int)Math.Ceiling(sorted.Count / (double)query.PageSize)),
            },
        });
    }

    // ---------------- Audit report (Admin only) ----------------
    public async Task<ReportOutput> AuditReportAsync(AuditReportQuery query)
    {
        var logs = await _auditLogs.BuildQueryAsync(query);

        var sortColumns = new Dictionary<string, System.Linq.Expressions.Expression<Func<AuditLog, object?>>>
        {
            ["createdAt"] = a => a.CreatedDate,
            ["action"] = a => a.Action,
            ["entityName"] = a => a.EntityName,
            ["userName"] = a => a.UserName,
            ["result"] = a => a.Result,
        };
        var sorted = ((IOrderedQueryable<AuditLog>)logs.SortBy(query, sortColumns, "createdAt")).ThenByDescending(a => a.AuditLogId);

        if (!query.IsCsv) return Json(await sorted.ToPagedResultAsync(query, AuditLogDto.From));

        var rows = (await sorted.ToListAsync()).Select(AuditLogDto.From);
        return Csv("audit-report.csv", CsvHelper.Build(rows,
            new CsvHelper.Column<AuditLogDto>("Date", r => r.CreatedAt),
            new CsvHelper.Column<AuditLogDto>("User", r => r.UserName),
            new CsvHelper.Column<AuditLogDto>("Action", r => r.Action),
            new CsvHelper.Column<AuditLogDto>("Module", r => r.EntityName),
            new CsvHelper.Column<AuditLogDto>("Record Id", r => r.RecordId),
            new CsvHelper.Column<AuditLogDto>("Result", r => r.Result),
            new CsvHelper.Column<AuditLogDto>("Details", r => r.Details),
            new CsvHelper.Column<AuditLogDto>("IP Address", r => r.IpAddress)));
    }

    // ---------------- helpers ----------------

    // Role scope + optional "assignedTo" filter - common to most reports
    private async Task<IQueryable<T>> ApplyCommonFiltersAsync<T>(IQueryable<T> query, ReportQuery filters) where T : class, IAssignable
    {
        var scoped = await _scope.ApplyScopeAsync(query);
        if (filters.AssignedTo is > 0) scoped = scoped.Where(x => x.AssignedTo == filters.AssignedTo);
        return scoped;
    }

    // dateFrom / dateTo on a created date column (India time days)
    private static IQueryable<T> FilterCreatedDate<T>(IQueryable<T> query, ReportQuery filters,
        System.Linq.Expressions.Expression<Func<T, DateTime>> dateColumn)
    {
        var (from, to) = QueryFilters.ReadRange(filters.DateFrom, filters.DateTo);
        var fromUtc = QueryFilters.StartUtc(from);
        var toUtc = QueryFilters.EndUtcExclusive(to);

        if (fromUtc != null) query = query.Where(WhereDate(dateColumn, fromUtc.Value, greaterOrEqual: true));
        if (toUtc != null) query = query.Where(WhereDate(dateColumn, toUtc.Value, greaterOrEqual: false));
        return query;
    }

    // Builds "x => x.Date >= value" or "x => x.Date < value" for the given column
    private static System.Linq.Expressions.Expression<Func<T, bool>> WhereDate<T>(
        System.Linq.Expressions.Expression<Func<T, DateTime>> column, DateTime value, bool greaterOrEqual)
    {
        var constant = System.Linq.Expressions.Expression.Constant(value);
        var body = greaterOrEqual
            ? System.Linq.Expressions.Expression.GreaterThanOrEqual(column.Body, constant)
            : System.Linq.Expressions.Expression.LessThan(column.Body, constant);
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, column.Parameters);
    }

    private static IEnumerable<UserActivityRow> SortUserActivity(IEnumerable<UserActivityRow> rows, ReportQuery query)
    {
        Func<UserActivityRow, object?> key = query.SortBy switch
        {
            "userName" => r => r.UserName,
            "role" => r => r.Role,
            "logins" => r => r.Logins,
            "failedLogins" => r => r.FailedLogins,
            "creates" => r => r.Creates,
            "updates" => r => r.Updates,
            "deletes" => r => r.Deletes,
            "lastActivity" => r => r.LastActivity,
            _ => r => r.TotalActions,
        };
        return query.Ascending ? rows.OrderBy(key) : rows.OrderByDescending(key);
    }

    private static double Percent(int part, int total) =>
        total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);

    private static decimal Round(decimal value) => Math.Round(value, 2);

    private static ReportOutput Json(object data) => new() { Data = data };

    private static ReportOutput Csv(string fileName, byte[] bytes) => new() { CsvBytes = bytes, FileName = fileName };
}
