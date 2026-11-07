using System.Net.Http.Json;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public sealed class ResultClient : IDisposable
{
    private readonly HttpClient _http;

    public ResultClient(string serverUrl, HttpMessageHandler? handler = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.BaseAddress = new Uri(serverUrl.TrimEnd('/') + '/');
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<long> SendAsync(RunResult run, CancellationToken cancellation = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/runs", run, ContractJson.Options, cancellation);
            return await ReadRunIdAsync(response, cancellation);
        }
        catch (HttpRequestException ex)
        {
            throw new ResultClientException($"сервер недоступний: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
        {
            throw new ResultClientException("сервер не відповів вчасно", ex);
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    private static async Task<long> ReadRunIdAsync(HttpResponseMessage response, CancellationToken cancellation)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync(cancellation);
            throw new ResultClientException($"сервер відповів {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<RunCreated>(ContractJson.Options, cancellation);
        return created?.RunId ?? throw new ResultClientException("порожня відповідь сервера");
    }
}
