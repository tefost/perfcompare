namespace PerfCompare.Server.Api;

public sealed record ErrorsResponse(IReadOnlyList<string> Errors)
{
    public static ErrorsResponse Of(string message)
    {
        return new ErrorsResponse(new[] { message });
    }
}
