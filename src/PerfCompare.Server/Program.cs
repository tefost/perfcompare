using PerfCompare.Server.Analytics;
using PerfCompare.Server.Api;
using PerfCompare.Server.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(provider =>
{
    var path = provider.GetRequiredService<IConfiguration>()["Database:Path"] ?? "perfcompare.db";
    return new SqliteConnectionFactory($"Data Source={path};Pooling=False");
});
builder.Services.AddSingleton<RunRepository>();
builder.Services.AddSingleton<BaselineRepository>();
builder.Services.AddSingleton<ComparisonService>();

var app = builder.Build();

DatabaseInitializer.Initialize(app.Services.GetRequiredService<SqliteConnectionFactory>());

app.MapRunsEndpoints();
app.MapStatsEndpoints();
app.MapComparisonEndpoints();

await app.RunAsync();

#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118
