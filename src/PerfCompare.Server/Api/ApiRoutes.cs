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
}
