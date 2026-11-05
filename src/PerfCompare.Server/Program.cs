var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await app.RunAsync();

#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118
