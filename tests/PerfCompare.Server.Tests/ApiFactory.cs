using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PerfCompare.Server.Tests;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"perfcompare-api-{Guid.NewGuid():N}.db");

    public string DatabasePath => _path;

    public LogCollector Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Path"] = _path }));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // temp file, removed by the system later
        }
    }
}
