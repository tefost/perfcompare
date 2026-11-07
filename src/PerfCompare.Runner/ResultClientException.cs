namespace PerfCompare.Runner;

public sealed class ResultClientException : Exception
{
    public ResultClientException()
    {
    }

    public ResultClientException(string message)
        : base(message)
    {
    }

    public ResultClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
