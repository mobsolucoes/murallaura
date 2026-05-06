namespace HashtagWall.Infrastructure.Instagram;

public sealed class GraphApiException : Exception
{
    public string? RawBody { get; }

    public GraphApiException(string message, string? rawBody = null, Exception? inner = null)
        : base(message, inner)
    {
        RawBody = rawBody;
    }
}
