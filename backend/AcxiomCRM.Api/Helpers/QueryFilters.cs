namespace AcxiomCRM.Api.Helpers;

// Reads date filters (?dateFrom=YYYY-MM-DD&dateTo=YYYY-MM-DD) from the query string
public static class QueryFilters
{
    public static DateOnly? ReadDate(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateHelper.Parse(value.Trim()) ?? throw AppException.BadRequest($"Invalid {name}. Use YYYY-MM-DD format.");
    }

    public static (DateOnly? From, DateOnly? To) ReadRange(string? dateFrom, string? dateTo)
    {
        var from = ReadDate(dateFrom, "dateFrom");
        var to = ReadDate(dateTo, "dateTo");
        if (from != null && to != null && from > to)
        {
            throw AppException.BadRequest("Date From cannot be after Date To.");
        }
        return (from, to);
    }

    // Start of "from" day in UTC (inclusive) - for DateTime columns like CreatedDate
    public static DateTime? StartUtc(DateOnly? from) => from == null ? null : DateHelper.LocalDateToUtc(from.Value);

    // Start of the day after "to" in UTC (exclusive)
    public static DateTime? EndUtcExclusive(DateOnly? to) => to == null ? null : DateHelper.LocalDateToUtc(to.Value.AddDays(1));

    public static bool IsCsv(string? format) => string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);
}
