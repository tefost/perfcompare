using System.Globalization;

namespace PerfCompare.Server.Api;

public static class CsvWriter
{
    public const string NewLine = "\r\n";

    private static readonly char[] Special = { ',', '"', '\r', '\n' };

    public static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var text = value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
        return text.IndexOfAny(Special) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }

    public static string Number(double? value)
    {
        return value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty;
    }

    public static string Integer(long? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }
}
