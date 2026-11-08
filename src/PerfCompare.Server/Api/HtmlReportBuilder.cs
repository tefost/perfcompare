using System.Globalization;
using System.Net;
using System.Text;
using PerfCompare.Server.Analytics;

namespace PerfCompare.Server.Api;

public static class HtmlReportBuilder
{
    private const string Dash = "-";

    public static string Build(
        ComparisonResult comparison,
        IReadOnlyDictionary<long, RegressionReport?> regressions,
        double thresholdPercent,
        DateTimeOffset generatedAt)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"uk\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.Append("<title>PerfCompare: ").Append(Encode(comparison.Scenario)).AppendLine("</title>");
        html.AppendLine("<style>body{font-family:system-ui,sans-serif;margin:2rem;color:#1f2933}"
            + "table{border-collapse:collapse;width:100%}th,td{border:1px solid #cbd2d9;padding:.4rem .6rem;text-align:left}"
            + "th{background:#f0f4f8}.bad{color:#b42318;font-weight:600}.good{color:#067647}</style>");
        html.AppendLine("</head><body>");
        html.Append("<h1>Звіт PerfCompare: ").Append(Encode(comparison.Scenario)).AppendLine("</h1>");
        html.Append("<p>Сформовано ")
            .Append(Encode(generatedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)))
            .Append(". Поріг регресії: ")
            .Append(Encode(Number(thresholdPercent)))
            .AppendLine("%.</p>");

        if (comparison.Items.Count == 0)
        {
            html.AppendLine("<p>Для цього сценарію ще немає запусків.</p>");
        }
        else
        {
            AppendTable(html, comparison, regressions);
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static void AppendTable(
        StringBuilder html, ComparisonResult comparison, IReadOnlyDictionary<long, RegressionReport?> regressions)
    {
        html.AppendLine("<table><thead><tr><th>Середовище</th><th>Запуск</th><th>Медіана, мс</th><th>P95, мс</th>"
            + "<th>Стабільність</th><th>Успішність, %</th><th>Повільніше за найшвидше</th><th>Регресія</th></tr></thead><tbody>");
        foreach (var item in comparison.Items)
        {
            var duration = item.Stats.Duration;
            regressions.TryGetValue(item.RunId, out var report);
            html.Append("<tr>");
            Cell(html, Describe(item.Environment.Os, item.Environment.OsVersion, item.Environment.Cores));
            Cell(html, item.RunId.ToString(CultureInfo.InvariantCulture));
            Cell(html, duration is null ? Dash : Number(duration.Median));
            Cell(html, duration is null ? Dash : Number(duration.P95));
            Cell(html, Number(item.Stats.StabilityIndex));
            Cell(html, Number(item.Stats.SuccessRate * 100));
            Cell(html, item.SlowdownVsFastest is double slowdown ? Number(slowdown) + "x" : Dash);
            html.Append(RegressionCell(report));
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");
    }

    private static string RegressionCell(RegressionReport? report)
    {
        if (report is null || report.Status == RegressionStatus.NoBaseline)
        {
            return "<td>немає базової лінії</td>";
        }

        if (report.Status == RegressionStatus.Regression)
        {
            var worst = report.Metrics.Where(metric => metric.IsRegression).Select(metric => metric.ChangePercent).DefaultIfEmpty(0).Max();
            return "<td class=\"bad\">регресія +" + Encode(Number(worst)) + "%</td>";
        }

        return "<td class=\"good\">без регресії</td>";
    }

    private static void Cell(StringBuilder html, string text)
    {
        html.Append("<td>").Append(Encode(text)).Append("</td>");
    }

    private static string Describe(string os, string version, int cores)
    {
        return $"{os} {version}, {cores.ToString(CultureInfo.InvariantCulture)} ядер";
    }

    private static string Number(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string Encode(string text)
    {
        return WebUtility.HtmlEncode(text);
    }
}
