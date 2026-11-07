using System.Text.Json;
using PerfCompare.Contracts;

namespace PerfCompare.Server.Api;

public static class RequestBody
{
    public static async Task<T?> ReadAsync<T>(HttpRequest request, CancellationToken cancellation)
        where T : class
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(request.Body, ContractJson.Options, cancellation);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
