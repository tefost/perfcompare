using System.Collections.Concurrent;

namespace PerfCompare.Server.Api;

public static class DashboardEndpoints
{
    public const string IndexName = "index.html";

    public static readonly IReadOnlyList<string> AssetNames = new[] { IndexName, "app.js", "style.css", "chart.umd.js" };

    private static readonly ConcurrentDictionary<string, byte[]?> Cache = new();

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Separator, () => Serve(IndexName));
        foreach (var name in AssetNames)
        {
            app.MapGet(ApiRoutes.Separator + name, () => Serve(name));
        }

        return app;
    }

    private static IResult Serve(string name)
    {
        var bytes = Cache.GetOrAdd(name, Load);
        return bytes is null ? Results.NotFound() : Results.Bytes(bytes, ContentTypeFor(name));
    }

    private static byte[]? Load(string name)
    {
        var assembly = typeof(DashboardEndpoints).Assembly;
        var resource = Array.Find(
            assembly.GetManifestResourceNames(),
            item => item.EndsWith("." + name, StringComparison.Ordinal));
        if (resource is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resource);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string ContentTypeFor(string name)
    {
        return Path.GetExtension(name) switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            _ => "application/octet-stream"
        };
    }
}
