namespace PerfCompare.Runner.Tests;

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public List<string> Bodies { get; } = new();

    public List<Uri?> Uris { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Uris.Add(request.RequestUri);
        if (request.Content is not null)
        {
            Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        return _respond(request);
    }
}
