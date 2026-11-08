using System.Globalization;

namespace PerfCompare.Server.Api;

public static class ApiRoutes
{
    public const string Separator = "/";
    public const string Runs = "/api/runs";
    public const string RunById = "/api/runs/{id:long}";
    public const string RunStats = "/api/runs/{id:long}/stats";
    public const string Baselines = "/api/baselines";
    public const string Compare = "/api/compare";
    public const string Regressions = "/api/regressions";
    public const string Scenarios = "/api/scenarios";
    public const string ExportCsv = "/api/export.csv";
    public const string ReportHtml = "/api/report.html";

    public static string RunLocation(long id)
    {
        return Runs + Separator + id.ToString(CultureInfo.InvariantCulture);
    }

    public static string RunStatsLocation(long id)
    {
        return RunLocation(id) + Separator + "stats";
    }

    public static string CompareUrl(long scenarioId, string? environmentIds = null)
    {
        var url = Compare + "?scenarioId=" + scenarioId.ToString(CultureInfo.InvariantCulture);
        return environmentIds is null ? url : url + "&environmentIds=" + environmentIds;
    }

    public static string RegressionsUrl(long runId, string? threshold = null)
    {
        var url = Regressions + "?runId=" + runId.ToString(CultureInfo.InvariantCulture);
        return threshold is null ? url : url + "&threshold=" + threshold;
    }

    public static string ExportUrl(long scenarioId)
    {
        return ExportCsv + "?scenarioId=" + scenarioId.ToString(CultureInfo.InvariantCulture);
    }

    public static string ReportUrl(long scenarioId, string? threshold = null)
    {
        var url = ReportHtml + "?scenarioId=" + scenarioId.ToString(CultureInfo.InvariantCulture);
        return threshold is null ? url : url + "&threshold=" + threshold;
    }
}
