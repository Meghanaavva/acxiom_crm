namespace AcxiomCRM.Api.Helpers;

public static class DateHelper
{
    // Business dates (follow-up date, close date) are compared in India time
    private static readonly TimeZoneInfo IndiaZone = FindZone();

    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.Local; }
    }

    public static DateTime NowLocal() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaZone);

    public static DateOnly Today() => DateOnly.FromDateTime(NowLocal());

    // Start of the given local day converted to UTC (for filtering CreatedDate columns)
    public static DateTime LocalDateToUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), IndiaZone);

    public static DateOnly ToLocalDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), IndiaZone));

    // UTC value from the database -> India time for display
    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), IndiaZone);

    public static DateOnly? Parse(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out var d) ? d : null;
}
