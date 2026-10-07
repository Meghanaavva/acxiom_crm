using System.Text;

namespace AcxiomCRM.Api.Helpers;

public static class CsvHelper
{
    public record Column<T>(string Label, Func<T, object?> Value);

    public static byte[] Build<T>(IEnumerable<T> rows, params Column<T>[] columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => Escape(c.Label))));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", columns.Select(c => Escape(c.Value(row)))));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(object? value)
    {
        if (value == null) return string.Empty;
        var text = value switch
        {
            DateTime d => d.ToString("yyyy-MM-dd HH:mm"),
            DateOnly d => d.ToString("yyyy-MM-dd"),
            bool b => b ? "Yes" : "No",
            _ => value.ToString() ?? string.Empty,
        };
        // prevent CSV formula injection when opened in Excel
        if (text.Length > 0 && "=+-@".Contains(text[0])) text = "'" + text;
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? "\"" + text.Replace("\"", "\"\"") + "\""
            : text;
    }
}
